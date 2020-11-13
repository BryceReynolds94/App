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
using System.Threading.Tasks;
using TeleSharp.TL;
using TeleSharp.TL.Account;
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

        private static readonly int API_ID = ***REMOVED***;
        private static readonly string API_HASH = "***REMOVED***";
        //TODO: Hide these values from potential attackers

        private string clientRequestCodeHash = null;
        private string clientPhoneNumber = null;

        public enum STATUS { NEW, OFFLINE, WAIT_PHONE, WAIT_CODE, AUTHORISED };

        public enum MESSAGING_KEYS { USER_DATA_CHANGED};

        public CommunicationService() {
            connectClient();
        }

        public async Task<bool> reloadConnection() {
            if(clientStatus == STATUS.OFFLINE || clientStatus == STATUS.NEW) {
                return await connectClient();
            }
            return true;
        }

        public event EventHandler StatusChanged;

        private async Task<bool> connectClient() {
            clientStatus = STATUS.OFFLINE;
            try {
                client = new TelegramClient(API_ID, API_HASH, new MySessionStore(this));
            } catch (Exception e) {
                Logger.Error(e, "Initialisation of TelegramClient failed");
                return false;
            }

            await client.ConnectAsync();
            clientStatus = STATUS.NEW;

            if (client.IsUserAuthorized()) {
                Logger.Debug("User is authorised allready.");
                clientStatus = STATUS.AUTHORISED;
                saveUserData(await getUser());

                string token = await CrossFirebasePushNotification.Current.GetTokenAsync();
                //await subscribePushNotifications(token); //TODO: Only do this on login?
            } else {
                Logger.Debug("User is not authorised. Awaiting login data.");
                clientStatus = STATUS.WAIT_PHONE;
            }
            return true;
        }

        public async Task subscribePushNotifications(string token) {
            if (clientStatus != STATUS.AUTHORISED) {
                Logger.Warn("Attempted to subscribe to FCM Messages without authorisation.");
                return;
            }

            TLRequestRegisterDevice request = new TLRequestRegisterDevice() {
                TokenType = 2, //2 = FCM, use  for APNs
                Token = token
            };

            try {
                await client.SendRequestAsync<bool>(request);
            } catch (Exception e) {
                Logger.Error(e, "Subscribing to push notifications failed");
            }

        }

        public async Task<TStatus> requestCode(string phoneNumber) {
            if (clientStatus != STATUS.WAIT_PHONE) {
                Logger.Warn("Attempted to register phone number without appropriate client status. Current status: " + clientStatus.ToString());
                return TStatus.WRONG_CLIENT_STATUS;
            }

            clientPhoneNumber = phoneNumber;

            //TODO: check if user is registered first
            //await client.IsPhoneRegisteredAsync(phoneNumber);

            TLMethod requestCode = new TeleSharp.TL.Auth.TLRequestSendCode() {
                PhoneNumber = clientPhoneNumber,
                ApiId = API_ID,
                ApiHash = API_HASH
            };

            TeleSharp.TL.Auth.TLSentCode code;
            try {
                code = await client.SendRequestAsync<TeleSharp.TL.Auth.TLSentCode>(requestCode);
            } catch (Exception e) {

                TException exception = getTException(e.Message);
                switch (exception) {
                    case TException.API_ID_INVALID:
                    case TException.API_ID_PUBLISHED_FLOOD:
                        Logger.Error(e, "Fatal exception while trying to authenticate user.");
                        return TStatus.UNKNOWN;
                    case TException.NETWORK_MIGRATE_X:
                    case TException.PHONE_MIGRATE_X:
                        //TODO: Handle this. Should not really ever occur - test in debugging
                        break;
                    case TException.PHONE_NUMBER_BANNED:
                    case TException.PHONE_NUMBER_FLOOD:
                    case TException.PHONE_NUMBER_INVALID:
                    case TException.PHONE_PASSWORD_FLOOD:
                        Logger.Warn(e, "Authenticating user failed.");
                        return TStatus.INVALID_PHONE_NUMBER;
                    //TODO: Inform user that phone number is invalid/blocked
                    case TException.PHONE_PASSWORD_PROTECTED:
                        //TODO: Implement 2FA for password protected accounts
                        break;

                    case TException.AUTH_RESTART:
                        //TODO: Handle relogin scenario
                        break;
                    default:
                        Logger.Error(e, "Unknown exception while trying to authenticate user.");
                        return TStatus.UNKNOWN;
                }
                return TStatus.UNKNOWN;
            }
            clientRequestCodeHash = code.PhoneCodeHash;
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
            } catch (Exception e) {
                Logger.Error(e, "Authenticating user code failed.");
                return TStatus.UNKNOWN;
            }
            saveUserData(user);
            clientStatus = STATUS.AUTHORISED;
            //TODO: Implement subscribePushNotifications(CrossFirebasePushNotification.Current.Token);
            return TStatus.OK;
        }

        public async void saveUserData(TLUser user) {
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

            TLMethod requestDialogList = new TLRequestGetDialogs() {
                OffsetPeer = new TLInputPeerSelf(),
                Limit = 100
            };

            TLDialogs dialogs;
            try {
                dialogs = await client.SendRequestAsync<TLDialogs>(requestDialogList);
            } catch (Exception e) {
                Logger.Error(e, "Exception while trying to fetch chat list.");
                return new TLVector<TLAbsChat>();
            }

            return dialogs.Chats;
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
            }catch(Exception exception) {
                Logger.Error(exception, "Exception while trying to fetch profile pic.");
                return new TLFile();
            }
            return file;
        }

        public async Task<TLFile> getProfilePic(int chatID) {
            TLRequestGetChats request = new TLRequestGetChats() {
                Id = new TLVector<int>() { chatID }
            };

            TLVector<TLChat> foundChats;
            try {
                foundChats = await client.SendRequestAsync<TLVector<TLChat>>(request);
            }catch(Exception exception) {
                Logger.Error(exception, "Exception while retrieving chat from chatID.");
                return new TLFile();
            }

            TLFileLocation loc = (foundChats.First().Photo as TLChatPhoto).PhotoSmall as TLFileLocation;
            TLFile file = await getProfilePic(loc);
            return file;
        }

        public async Task<TLUser> getUser() {

            TLRequestGetUsers request = new TLRequestGetUsers() {
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

        public async Task<TLAbsMessages> getMessages(int chatID, int lastMessageID) {
            if (clientStatus != STATUS.AUTHORISED) {
                Logger.Warn("Attempting to get messages without user authorisation. Current status: " + clientStatus.ToString());
                return new TLMessages();
            }

            TLRequestGetHistory request = new TLRequestGetHistory() {
                Peer = new TLInputPeerChat() {ChatId = chatID},
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

            public static string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "session");
            public void Save(Session session) {

                using (FileStream fileStream = new FileStream(string.Format(file, (object)session.SessionUserId), FileMode.OpenOrCreate)) {
                    byte[] bytes = session.ToBytes();
                    fileStream.Write(bytes, 0, bytes.Length);
                }
            }

            public Session Load(string sessionUserId) {

                string path = string.Format(file, (object)sessionUserId);
                if (!File.Exists(path))
                    return (Session)null;

                var buffer = File.ReadAllBytes(path);
                Session session = Session.FromBytes(buffer, this, sessionUserId);
                client.user = session.TLUser;
                return session;
            }
        }


    }
}
