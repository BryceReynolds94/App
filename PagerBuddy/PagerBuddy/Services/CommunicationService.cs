using PagerBuddy.Models;
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
using static PagerBuddy.Services.ClientExceptions;

namespace PagerBuddy.Services {
    public class CommunicationService {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private TelegramClient client;
        private TLUser user;

        private CommunicationServiceQueue queue;

        private STATUS status;
        public STATUS clientStatus {
            get {
                return status;
            }
            private set {
                status = value;
                StatusChanged?.Invoke(this, value);
                Logger.Info("Status of CommunicationService changed to " + value.ToString());
            }
        }

        private string clientRequestCodeHash = null;
        private string clientPhoneNumber = null;

        public enum STATUS { NEW, OFFLINE, ONLINE, WAIT_PHONE, WAIT_CODE, WAIT_PASSWORD, AUTHORISED };

        public enum MESSAGING_KEYS { USER_DATA_CHANGED };

        public CommunicationService(bool isBackgroundCall = false) {
            Logger.Debug("Initialising CommunicationService. Is background call: " + isBackgroundCall);
            queue = new CommunicationServiceQueue();
            _ = connectClient(isBackgroundCall);
        }

        public async Task reloadConnection() {
            if (clientStatus == STATUS.OFFLINE) { //Do not reload if we are currently connecting or have successfully connected
                Logger.Info("Reloading connection.");
                await connectClient();
            }
        }

        public async Task forceReloadConnection(bool isBackgroundCall = false) {
            client.Dispose();
            await connectClient(isBackgroundCall);
        }

        public event ClientStausEventHandler StatusChanged;
        public delegate void ClientStausEventHandler(object sender, STATUS newStatus);

        private async Task connectClient(bool isBackgroundCall = false) {
            clientStatus = STATUS.NEW;
            try {
                client = new TelegramClient(KeyService.checkID(this), KeyService.checkHash(this), new MySessionStore(this));
            } catch (Exception e) {
                Logger.Error(e, "Initialisation of TelegramClient failed");
                clientStatus = STATUS.OFFLINE;
                scheduleRetry(isBackgroundCall);
                return;
            }

            if(! await tryConnect(isBackgroundCall)) {
                clientStatus = STATUS.OFFLINE;
                scheduleRetry(isBackgroundCall);
                return;
            }

            clientStatus = STATUS.ONLINE;

            if (client.IsUserAuthorized()) {
                Logger.Debug("User is authorised already.");
                this.user = await getUserUpdate(this.user);
                if (!isBackgroundCall) { //only do this stuff if we are not retrieving alert messages
                    await saveUserData(user);
                    //Update current message index
                    DataService.setConfigValue(DataService.DATA_KEYS.LAST_MESSAGE_ID, await getLastMessageID(0, true));
                }
                clientStatus = STATUS.AUTHORISED;
            } else {
                Logger.Debug("User is not authorised. Awaiting login data.");
                clientStatus = STATUS.WAIT_PHONE;
            }
        }

        private void scheduleRetry(bool isBackgroundCall) {
            if (isBackgroundCall) {
                Logger.Warn("Client called in background. Not scheduling another connection attempt.");
                return;
            }
            Logger.Warn("Could not connect client. Will retry in 5 seconds.");

            //Retry in 5 seconds
            Task.Delay(5000).ContinueWith(t => reloadConnection());
        }

        private async Task<bool> connectTimeWatcher() {
            //In this version of TLSharp library we have a problem with (seldom) infinate loops
            //Apparently has been solved in newer versions - maybe update TLSharp some day

            bool wasKilled = false;
            await Task.WhenAny(client.ConnectAsync(), Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(t => wasKilled = true)); //giving connectasync max. 10s to return
            return wasKilled;
        }

        private async Task<bool> tryConnect(bool isBackgroundCall, int attempt = 0) {
            bool retry = false;
            try {
                bool wasKilled = await queue.Enqueue(new Func<Task<bool>>(async () => await connectTimeWatcher()));

                if (wasKilled) {
                    Logger.Error("Connecting client took too long and was cancelled.");
                    retry = true;
                }
            } catch (InvalidOperationException e) {
                //TODO: Testing - can we recover from this?
                Logger.Error(e, "Exception during connection attempt.");
                retry = true;
            } catch (Exception e) {
                Logger.Error(e, "Unknown exception during connection attempt");
                if (isBackgroundCall) {
                    Logger.Warn("Stoppig client initialisation after fatal error while called in background.");
                    return false;
                }

                Logger.Error("Fatal Problem with client. Clearing session store and starting again.");
                clientStatus = STATUS.NEW;
                new MySessionStore(this).Clear();
                await forceReloadConnection(isBackgroundCall);
                return false;
            }
            if (retry) {
                if (attempt > 2) {
                    Logger.Error("No success connecting within 3 attempts. Giving up.");
                    return false;
                }
                Logger.Warn("Trying again.");
                return await tryConnect(isBackgroundCall, ++attempt);
            }
            return true;
        }

        private async Task checkConnectionOnError(Exception e = null) {
            //TODO: Testing
            if(clientStatus > STATUS.OFFLINE) {
                if (client.IsConnected) {
                    if (clientStatus == STATUS.AUTHORISED && !client.IsUserAuthorized()) {
                        //Something went very wrong - set offline as a recovery solution
                        Logger.Warn("Status set to authorised but user is not authorised. Force setting offline status.");
                        clientStatus = STATUS.OFFLINE;
                        await reloadConnection();
                    }
                } else {
                    Logger.Warn("Client is not connected. Force setting offline status.");
                    clientStatus = STATUS.OFFLINE;
                    await reloadConnection();
                }
            }
        }

        public async Task logoutUser() {
            if (clientStatus != STATUS.AUTHORISED) {
                Logger.Warn("Attempted to logout user without authorisation. Current state: " + clientStatus.ToString());
            }
            Logger.Debug("Logging out user.");

            string token = CrossFirebasePushNotification.Current.Token;

            TLRequestUnregisterDevice unregisterRequest = new TLRequestUnregisterDevice() {
                TokenType = 2,
                Token = token
            };

            try {
                await queue.Enqueue(new Func<Task>(async () => await client.SendRequestAsync<bool>(unregisterRequest))); //https://core.telegram.org/method/account.unregisterDevice
            } catch (Exception e) {
                Logger.Error(e, "Exception while trying to unregister device.");
            }

            try {
                await queue.Enqueue(new Func<Task>(async () => await client.SendRequestAsync<bool>(new TLRequestLogOut()))); //https://core.telegram.org/method/auth.logOut
            } catch (Exception e) {
                Logger.Error(e, "Exception while trying to logout user.");
            }

            clientStatus = STATUS.NEW;
            new MySessionStore(this).Clear();
            await forceReloadConnection();
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
                await queue.Enqueue(new Func<Task>(async () => await client.SendRequestAsync<bool>(request)));
            } catch (Exception e) {
                Logger.Error(e, "Subscribing to push notifications failed");
                await checkConnectionOnError(e);
            }

        }

        public async Task<TStatus> loginWithPassword(string password) {
            if (clientStatus != STATUS.WAIT_PASSWORD) {
                Logger.Warn("Attempted to perform 2FA without appropriate client status. Current status: " + clientStatus.ToString());
                return TStatus.WRONG_CLIENT_STATUS;
            }

            TLPassword passwordConfig;
            try {
                passwordConfig = await queue.Enqueue(new Func<Task<TLPassword>>(async () => await client.GetPasswordSetting()));
            } catch (Exception e) {
                Logger.Error(e, "Exception occured while trying to receive password configuration");
                await checkConnectionOnError(e);
                return TStatus.UNKNOWN;
            }

            TLUser user;
            try {
                user = await queue.Enqueue(new Func<Task<TLUser>>(async () => await client.MakeAuthWithPasswordAsync(passwordConfig, password)));
            } catch (Exception e) {
                TException exception = getTException(e.Message);
                switch (exception) {
                    case TException.PASSWORD_HASH_INVALID:
                        Logger.Info(e, "Invalid password entered.");
                        return TStatus.INVALID_PASSWORD;
                    default:
                        Logger.Error(e, "Unknown exception occured while trying to perform authentication with password");
                        await checkConnectionOnError(e);
                        return TStatus.UNKNOWN;
                }
            }

            await loginCompleted(user);
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
                hash = await queue.Enqueue(new Func<Task<string>>(async () => await client.SendCodeRequestAsync(phoneNumber)));
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
                        await checkConnectionOnError(e);
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

            TLUser user;
            try {
                user = await queue.Enqueue(new Func<Task<TLUser>>(async () => await client.MakeAuthAsync(clientPhoneNumber, clientRequestCodeHash, code)));
            } catch (CloudPasswordNeededException e) {
                Logger.Info(e, "Two factor authentication needed.");
                clientStatus = STATUS.WAIT_PASSWORD;
                return TStatus.PASSWORD_REQUIRED;
            } catch (InvalidPhoneCodeException e) {
                Logger.Info(e, "Incorrect code entered.");
                return TStatus.INVALID_CODE;
            } catch (Exception e) {
                Logger.Error(e, "Authenticating user code failed.");
                await checkConnectionOnError(e);
                return TStatus.UNKNOWN;
            }

            await loginCompleted(user);
            return TStatus.OK;
        }

        private async Task loginCompleted(TLUser user) {
            this.user = user;
            await saveUserData(user);
            clientStatus = STATUS.AUTHORISED;

            string token = await CrossFirebasePushNotification.Current.GetTokenAsync();
            if (token.Length > 1) {
                await subscribePushNotifications(token);
            } else {
                Logger.Warn("Could not subscribe to FCM Messages as no token available");
            }
            //set current message id
            DataService.setConfigValue(DataService.DATA_KEYS.LAST_MESSAGE_ID, await getLastMessageID(0));
        }

        private async Task saveUserData(TLUser user) {
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
                dialogs = await queue.Enqueue(new Func<Task<TLAbsDialogs>>(async () => await client.SendRequestAsync<TLAbsDialogs>(requestDialogList)));
            } catch (Exception e) {
                Logger.Error(e, "Exception while trying to fetch chat list.");
                await checkConnectionOnError(e);
                return new TLVector<TLAbsChat>();
            }

            if (!(dialogs is TLDialogs)) {
                Logger.Error("Unexpected return Type while fetching dialogs. Type: " + dialogs.GetType());
                return new TLVector<TLAbsChat>();
            }

            return (dialogs as TLDialogs).Chats;
        }

        public async Task<TLFile> getProfilePic(TLFileLocation location) {
            if (clientStatus < STATUS.ONLINE) {
                Logger.Warn("Attempted to load profile pic without appropriate client status. Current status: " + clientStatus.ToString());
                return new TLFile();
            }
            TLInputFileLocation loc = new TLInputFileLocation() {
                LocalId = location.LocalId,
                Secret = location.Secret,
                VolumeId = location.VolumeId
            };

            TLFile file;
            try {
                file = await queue.Enqueue(new Func<Task<TLFile>>(async () => await client.GetFile(loc, 1024 * 256)));
            } catch (Exception exception) {
                Logger.Error(exception, "Exception while trying to fetch profile pic.");
                await checkConnectionOnError(exception);
                return new TLFile();
            }
            return file;
        }

        private async Task<TLUser> getUserUpdate(TLUser user) {
            if (clientStatus < STATUS.ONLINE) {
                Logger.Warn("Attempted to retrieve user update without appropriate client status. Current status: " + clientStatus.ToString());
                return new TLUser();
            }

            TLRequestGetUsers request = new TLRequestGetUsers() { //https://core.telegram.org/method/users.getUsers
                Id = new TLVector<TLAbsInputUser> { new TLInputUser() { UserId = user.Id, AccessHash = (long)user.AccessHash } }
            };

            TLUser outUser;
            try {
                TLVector<TLAbsUser> result = await queue.Enqueue(new Func<Task<TLVector<TLAbsUser>>>(async () => await client.SendRequestAsync<TLVector<TLAbsUser>>(request)));
                outUser = result.First() as TLUser;
            } catch (Exception e) {
                Logger.Error(e, "Exception while fetching user data.");
                await checkConnectionOnError(e);
                return new TLUser();
            }

            return outUser;
        }

        public async Task<int> getLastMessageID(int currentID, bool init = false) {
            if ((clientStatus != STATUS.AUTHORISED && !init) || clientStatus < STATUS.ONLINE) {
                Logger.Warn("Attempted to get last message ID with inappropriate client status. Current status: " + clientStatus.ToString());
                return currentID;
            }

            TLMethod requestDialogList = new TLRequestGetDialogs() { //https://core.telegram.org/method/messages.getDialogs
                OffsetPeer = new TLInputPeerSelf(),
                Limit = 100
            };

            TLAbsDialogs dialogs;
            try {
                dialogs = await queue.Enqueue(new Func<Task<TLAbsDialogs>>(async () => await client.SendRequestAsync<TLAbsDialogs>(requestDialogList)));
            } catch (Exception e) {
                Logger.Error(e, "Exception while trying to fetch chat list for message IDs.");
                await checkConnectionOnError(e);
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
                return null;
            }

            TLRequestGetHistory request = new TLRequestGetHistory() { //https://core.telegram.org/method/messages.getHistory
                Peer = new TLInputPeerChat() { ChatId = chatID },
                Limit = 100,
                MinId = lastMessageID
            };

            TLAbsMessages messages;
            try {
                messages = await queue.Enqueue(new Func<Task<TLAbsMessages>>(async () => await client.SendRequestAsync<TLAbsMessages>(request)));
            } catch (Exception e) {
                Logger.Error(e, "Exception while trying to retrieve messages.");
                await checkConnectionOnError(e);
                return null;
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
