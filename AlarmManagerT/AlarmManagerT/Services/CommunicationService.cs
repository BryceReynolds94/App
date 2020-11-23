using AlarmManagerT.Models;
using Newtonsoft.Json.Linq;
using Plugin.FirebasePushNotification;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeleSharp.TL;
using TeleSharp.TL.Account;
using TeleSharp.TL.Auth;
using TeleSharp.TL.Messages;
using TeleSharp.TL.Updates;
using TeleSharp.TL.Upload;
using TeleSharp.TL.Users;
using TLSharp.Core;
using Xamarin.Forms;
using static AlarmManagerT.Services.ClientExceptions;

namespace AlarmManagerT.Services {
    public class CommunicationService {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private TelegramClient client;
        private TLUser user;

        private STATUS status;
        public STATUS clientStatus {
            get {
                return status;
            }
            private set {
                status = value;
                StatusChanged?.Invoke(this, null);
                Logger.Info("Status of CommunicationService changed to " + value.ToString());
            }
        }

        private string clientRequestCodeHash = null;
        private string clientPhoneNumber = null;

        public enum STATUS { NEW, OFFLINE, WAIT_PHONE, WAIT_CODE, WAIT_PASSWORD, AUTHORISED };

        public enum MESSAGING_KEYS { USER_DATA_CHANGED };

        public CommunicationService() {
            _ = connectClient();
        }

        public async Task reloadConnection() {
            if (clientStatus == STATUS.OFFLINE || clientStatus == STATUS.NEW) {
                await connectClient();
            }
        }

        public async Task forceReloadConnection() {
            client.Dispose();
            await connectClient();
        }

        public event EventHandler StatusChanged;

        private async Task connectClient() {
            clientStatus = STATUS.OFFLINE;
            try {
                client = new TelegramClient(KeyService.checkID(this), KeyService.checkHash(this), new MySessionStore(this));
            } catch (Exception e) {
                Logger.Error(e, "Initialisation of TelegramClient failed");
                return;
            }

            try {
                await client.ConnectAsync();
            } catch (Exception e) {
                Logger.Error(e, "Exception during connection attempt");
                Logger.Error("Fatal Problem with client. Clearing session store and starting again.");

                new MySessionStore(this).Clear();
                _ = connectClient();
                return;
            }
            clientStatus = STATUS.NEW;


            if (client.IsUserAuthorized()) {
                Logger.Debug("User is authorised allready.");
                clientStatus = STATUS.AUTHORISED;
                await saveUserData(await getUser());
                //Update current message index
                DataService.setConfigValue(DataService.DATA_KEYS.LAST_MESSAGE_ID, await getLastMessageID(0));
            } else {
                Logger.Debug("User is not authorised. Awaiting login data.");
                clientStatus = STATUS.WAIT_PHONE;
            }
        }

        public async Task logoutUser() {
            if (clientStatus != STATUS.AUTHORISED) {
                Logger.Warn("Attempted to logout user without authorisation. Current state: " + clientStatus.ToString());
                return;
            }
            Logger.Debug("Loggin out user.");

            string token = await CrossFirebasePushNotification.Current.GetTokenAsync(); //TODO: Testing

            TLRequestUnregisterDevice unregisterRequest = new TLRequestUnregisterDevice() {
                TokenType = 2,
                Token = token
            };

            try {
                await client.SendRequestAsync<TLAbsBool>(unregisterRequest); //https://core.telegram.org/method/account.unregisterDevice
            } catch (Exception e) {
                Logger.Error(e, "Exception while trying to unregister device.");
            }

            try {
                await client.SendRequestAsync<TLAbsBool>(new TLRequestLogOut()); //https://core.telegram.org/method/auth.logOut
            } catch (Exception e) {
                Logger.Error(e, "Exception while trying to logout user.");
            }

            clientStatus = STATUS.NEW;

            new MySessionStore(this).Clear();

            await reloadConnection();
        }

        public async Task subscribePushNotifications(string token) {
            if (clientStatus != STATUS.AUTHORISED) {
                Logger.Warn("Attempted to subscribe to FCM Messages without authorisation.");
                return;
            }

            TLRequestRegisterDevice request = new TLRequestRegisterDevice() { //https://core.telegram.org/method/account.registerDevice
                TokenType = 2, //2 = FCM, use  for APNs
                Token = token
            };

            try {
                await client.SendRequestAsync<bool>(request);
            } catch (Exception e) {
                Logger.Error(e, "Subscribing to push notifications failed");
            }

        }

        public async Task<TStatus> loginWithPassword(string password) {
            if (clientStatus != STATUS.WAIT_PASSWORD) {
                Logger.Warn("Attempted to perform 2FA without appropriate client status. Current status: " + clientStatus.ToString());
                return TStatus.WRONG_CLIENT_STATUS;
            }

            TLPassword passwordConfig;
            try {
                passwordConfig = await client.GetPasswordSetting();
            } catch (Exception e) {
                Logger.Error(e, "Exception occured while trying to receive password configuration");
                return TStatus.UNKNOWN;
            }

            TLUser user;
            try {
                user = await client.MakeAuthWithPasswordAsync(passwordConfig, password);
            } catch (Exception e) {

                TException exception = getTException(e.Message);
                switch (exception) {
                    case TException.PASSWORD_HASH_INVALID:
                        Logger.Info(e, "Invalid password entered.");
                        return TStatus.INVALID_PASSWORD;
                    default:
                        Logger.Error(e, "Unknown exception occured while trying to perform authentication with password");
                        return TStatus.UNKNOWN;
                }
            }

            loginCompleted(user);
            return TStatus.OK;
        }

        public async Task<TStatus> requestCode(string phoneNumber) {
            if (clientStatus != STATUS.WAIT_PHONE && clientStatus != STATUS.WAIT_CODE && clientStatus != STATUS.WAIT_PASSWORD) {
                Logger.Warn("Attempted to register phone number without appropriate client status. Current status: " + clientStatus.ToString());
                return TStatus.WRONG_CLIENT_STATUS;
            }

            clientPhoneNumber = phoneNumber;

            string hash;
            try {
                hash = await client.SendCodeRequestAsync(phoneNumber);
            } catch (Exception e) {

                TException exception = getTException(e.Message);
                switch (exception) {
                    case TException.API_ID_INVALID:
                    case TException.API_ID_PUBLISHED_FLOOD:
                        Logger.Error(e, "Fatal exception while trying to authenticate user.");
                        return TStatus.UNKNOWN;
                    case TException.NETWORK_MIGRATE_X:
                    case TException.PHONE_MIGRATE_X:
                        Logger.Error(e, "Unexpected migration error while trying to authenticate user.");
                        return TStatus.UNKNOWN;
                    case TException.PHONE_NUMBER_BANNED:
                    case TException.PHONE_NUMBER_FLOOD:
                    case TException.PHONE_NUMBER_INVALID:
                    case TException.PHONE_PASSWORD_FLOOD:
                        Logger.Warn(e, "Authenticating user failed.");
                        return TStatus.INVALID_PHONE_NUMBER;

                    case TException.AUTH_RESTART:
                        Logger.Warn(e, "Authentication was restarted.");
                        return await requestCode(phoneNumber);
                    default:
                        Logger.Error(e, "Unknown exception while trying to authenticate user.");
                        return TStatus.UNKNOWN;
                }
            }
            clientRequestCodeHash = hash;
            clientStatus = STATUS.WAIT_CODE;

            return TStatus.OK;
        }

        public async Task<TStatus> confirmCode(string code) {
            if (clientStatus != STATUS.WAIT_CODE) {
                Logger.Warn("Attempted to confirm code without appropriate client status. Current status: " + clientStatus.ToString());
                return TStatus.WRONG_CLIENT_STATUS;
            }

            try {
                user = await client.MakeAuthAsync(clientPhoneNumber, clientRequestCodeHash, code);
            } catch (CloudPasswordNeededException e) {
                Logger.Info(e, "Two factor authentication needed.");
                clientStatus = STATUS.WAIT_PASSWORD;
                return TStatus.PASSWORD_REQUIRED;
            } catch (InvalidPhoneCodeException e) {
                Logger.Info(e, "Incorrect code entered.");
                return TStatus.INVALID_CODE;
            } catch (Exception e) {
                Logger.Error(e, "Authenticating user code failed.");
                return TStatus.UNKNOWN;
            }

            loginCompleted(user);
            return TStatus.OK;
        }

        private async void loginCompleted(TLUser user) {
            await saveUserData(user);
            clientStatus = STATUS.AUTHORISED;

            string token = CrossFirebasePushNotification.Current.Token;
            if (token.Length > 1) {
                await subscribePushNotifications(token);
            } else {
                Logger.Warn("Could not subscribe to FCM Messages as no token available");
            }
            //set current message id
            DataService.setConfigValue(DataService.DATA_KEYS.LAST_MESSAGE_ID, await getLastMessageID(0));
        }

        public async Task saveUserData(TLUser user) {
            if (user == null) {
                Logger.Error("Attempting to save null user.");
                return;
            }

            TLUserProfilePhoto photo = (user.Photo as TLUserProfilePhoto);
            bool hasPhoto = false;
            if (photo != null) {
                TLFile file = await getProfilePic(photo.PhotoBig as TLFileLocation);
                MemoryStream memoryStream = new MemoryStream(file.Bytes);

                DataService.saveProfilePic(DataService.DATA_KEYS.USER_PHOTO.ToString(), memoryStream);
                hasPhoto = true;
            }
            DataService.setConfigValue(DataService.DATA_KEYS.USER_HAS_PHOTO, hasPhoto);

            string userName = user.FirstName + " " + user.LastName;
            if (userName.Length < 3) {
                userName = user.Username;
            }
            string userPhone = "+" + user.Phone;

            DataService.setConfigValue(DataService.DATA_KEYS.USER_NAME, userName);
            DataService.setConfigValue(DataService.DATA_KEYS.USER_PHONE, userPhone);

            MessagingCenter.Send(this, MESSAGING_KEYS.USER_DATA_CHANGED.ToString());
        }

        public async Task<TLVector<TLAbsChat>> getChatList() {
            if (clientStatus != STATUS.AUTHORISED) {
                Logger.Warn("Attempted to load chat list without appropriate client status. Current status: " + clientStatus.ToString());
                return new TLVector<TLAbsChat>();
            }

            TLMethod requestDialogList = new TLRequestGetDialogs() { //https://core.telegram.org/method/messages.getDialogs
                OffsetPeer = new TLInputPeerSelf(),
                Limit = 100
            };

            TLAbsDialogs dialogs;
            try {
                dialogs = await client.SendRequestAsync<TLAbsDialogs>(requestDialogList);
            } catch (Exception e) {
                Logger.Error(e, "Exception while trying to fetch chat list.");
                return new TLVector<TLAbsChat>();
            }

            if (!(dialogs is TLDialogs)) {
                Logger.Error("Unexpected return Type while fetching dialogs. Type: " + dialogs.GetType());
                return new TLVector<TLAbsChat>();
            }

            return (dialogs as TLDialogs).Chats;
        }

        public async Task<TLFile> getProfilePic(TLFileLocation location) {
            TLInputFileLocation loc = new TLInputFileLocation() {
                LocalId = location.LocalId,
                Secret = location.Secret,
                VolumeId = location.VolumeId
            };

            TLFile file;
            try {
                file = await client.GetFile(loc, 1024 * 256);
            } catch (Exception exception) {
                Logger.Error(exception, "Exception while trying to fetch profile pic.");
                return new TLFile();
            }
            return file;
        }

        public async Task<TLFile> getProfilePic(int chatID) {
            TLRequestGetChats request = new TLRequestGetChats() { //https://core.telegram.org/method/messages.getChats
                Id = new TLVector<int>() { chatID }
            };

            TLVector<TLChat> foundChats;
            try {
                foundChats = await client.SendRequestAsync<TLVector<TLChat>>(request);
            } catch (Exception exception) {
                Logger.Error(exception, "Exception while retrieving chat from chatID.");
                return new TLFile();
            }

            TLFileLocation loc = (foundChats.First().Photo as TLChatPhoto).PhotoSmall as TLFileLocation;
            TLFile file = await getProfilePic(loc);
            return file;
        }

        public async Task<TLUser> getUser() {

            TLRequestGetUsers request = new TLRequestGetUsers() { //https://core.telegram.org/method/users.getUsers
                Id = new TLVector<TLAbsInputUser> { new TLInputUser() { UserId = user.Id, AccessHash = (long)user.AccessHash } }
            };

            TLVector<TLAbsUser> outUser;
            try {
                outUser = await client.SendRequestAsync<TLVector<TLAbsUser>>(request);
                user = outUser.First() as TLUser;
            } catch (Exception e) {
                Logger.Error(e, "Exception while fetching user data.");
                return new TLUser();
            }

            return user;
        }

        public async Task<int> getLastMessageID(int currentID) {
            if (clientStatus != STATUS.AUTHORISED) {
                Logger.Warn("Attempted to get last message ID with inappropriate client status. Current status: " + clientStatus.ToString());
                return currentID;
            }

            TLMethod requestDialogList = new TLRequestGetDialogs() { //https://core.telegram.org/method/messages.getDialogs
                OffsetPeer = new TLInputPeerSelf(),
                Limit = 100
            };

            TLAbsDialogs dialogs;
            try {
                dialogs = await client.SendRequestAsync<TLAbsDialogs>(requestDialogList);
            } catch (Exception e) {
                Logger.Error(e, "Exception while trying to fetch chat list for message IDs.");
                return currentID;
            }

            if (!(dialogs is TLDialogs)) {
                Logger.Error("Unexpected return Type while fetching dialogs. Type: " + dialogs.GetType());
                return currentID;
            }

            //first dialog has highest message id
            TLAbsMessage msg = (dialogs as TLDialogs).Messages[0];
            if (msg is TLMessage) {
                currentID = Math.Max((msg as TLMessage).Id, currentID);
            } else if (msg is TLMessageService) {
                currentID = Math.Max((msg as TLMessageService).Id, currentID);
            }
            return currentID;
        }

        public async Task<TLAbsMessages> getMessages(int chatID, int lastMessageID) {
            if (clientStatus != STATUS.AUTHORISED) {
                Logger.Warn("Attempting to get messages without user authorisation. Current status: " + clientStatus.ToString());
                return new TLMessages();
            }

            TLRequestGetHistory request = new TLRequestGetHistory() { //https://core.telegram.org/method/messages.getHistory
                Peer = new TLInputPeerChat() { ChatId = chatID },
                Limit = 100,
                MinId = lastMessageID + 1
            };

            TLAbsMessages messages;
            try {
                messages = await client.SendRequestAsync<TLAbsMessages>(request);
            } catch (Exception e) {
                Logger.Error(e, "Exception while trying to retrieve messages.");
                return new TLMessages();
            }
            return messages;
        }

        public class MySessionStore : ISessionStore {

            private CommunicationService client;
            public MySessionStore(CommunicationService client) {
                this.client = client;
            }

            public static string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "CommunicationServiceSession");
            public void Save(Session session) {

                using (FileStream fileStream = new FileStream(file, FileMode.OpenOrCreate)) {
                    byte[] bytes = session.ToBytes();
                    fileStream.Write(bytes, 0, bytes.Length);
                }
            }

            public Session Load(string sessionUserId) {
                ;
                if (!File.Exists(file)) {
                    return (Session)null;
                }

                var buffer = File.ReadAllBytes(file);
                Session session = Session.FromBytes(buffer, this, sessionUserId);

                client.user = session.TLUser;
                return session;
            }

            public void Clear() {
                if (File.Exists(file)) {
                    File.Delete(file);
                }
            }
        }


    }
}
