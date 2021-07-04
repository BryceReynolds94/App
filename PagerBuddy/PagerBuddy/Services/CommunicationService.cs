using PagerBuddy.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using static PagerBuddy.Services.ClientExceptions;
using Telega;
using Telega.Client;
using Functions = Telega.Rpc.Dto.Functions;
using Types = Telega.Rpc.Dto.Types;
using LanguageExt;
using System.Security.Cryptography;

namespace PagerBuddy.Services {

    public class CommunicationService {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private TelegramClient client;
        private STATUS status;
        public STATUS clientStatus {
            get {
                return status;
            }
            private set {
                status = value;
                Logger.Info("Status of CommunicationService changed to " + value.ToString());
                StatusChanged?.Invoke(this, value);
            }
        }

        private string clientRequestCodeHash = null;
        private string clientPhoneNumber = null;

        public enum STATUS { NEW, OFFLINE, ONLINE, WAIT_PHONE, WAIT_CODE, WAIT_PASSWORD, AUTHORISED };

        public enum MESSAGING_KEYS { USER_DATA_CHANGED };

        public CommunicationService(bool isBackgroundCall = false) {
            Logger.Debug("Initialising CommunicationService. Is background call: " + isBackgroundCall);
            _ = connectClient(isBackgroundCall);
        }

        public async Task reloadConnection(bool isBackgroundCall = false, int attempt = 0) {
            if (clientStatus == STATUS.OFFLINE) { //Do not reload if we are currently connecting or have successfully connected
                Logger.Info("Reloading connection.");
                await connectClient(isBackgroundCall, attempt);
            }
        }

        public async Task forceReloadConnection(bool isBackgroundCall = false) {
            client.Dispose();
            client = null;
            MySessionStore.Clear();

            await connectClient(isBackgroundCall);
        }

        public event ClientStausEventHandler StatusChanged;
        public delegate void ClientStausEventHandler(object sender, STATUS newStatus);

        private async Task connectClient(bool isBackgroundCall = false, int attempt = 0) {
            clientStatus = STATUS.NEW;
            try {
                client = await TelegramClient.Connect(KeyService.checkID(this), store: new MySessionStore());
            } catch (Exception e) {
                Logger.Error(e, "Initialisation of TelegramClient failed");

                if(e is TgException && e.Message.Contains("Invalid session file")) { //Handle corrupt session files. (Trautner Bug)
                    Logger.Warn("Session file was corrupted. Clearing session file.");
                    MySessionStore.Clear();
                }

                clientStatus = STATUS.OFFLINE;
                scheduleRetry(isBackgroundCall, attempt);
                return;
            }

            clientStatus = STATUS.ONLINE;

            if (client.Auth.IsAuthorized) {
                Logger.Debug("User is authorised.");
                if (!isBackgroundCall) { //only do this stuff if we are not retrieving alert messages
                    Types.User user = await getUserUpdate();
                    if (user != null) {
                        await saveUserData(user);
                    }
                    await subscribePushNotifications(DataService.getConfigValue(DataService.DATA_KEYS.FCM_TOKEN, ""), true);
                }
                if (clientStatus != STATUS.ONLINE) { //status may have changed out of scope due to previous call fallbacks
                    Logger.Info("Connect process completed, but client Status was changed out of scope. Not setting authorised status. CurrentStatus: " + clientStatus.ToString());
                }
                clientStatus = STATUS.AUTHORISED;
            } else {
                Logger.Debug("User is not authorised. Awaiting login data.");
                clientStatus = STATUS.WAIT_PHONE;
            }
        }

        private void scheduleRetry(bool isBackgroundCall, int attempt = 0) {
            if (isBackgroundCall && attempt > 2) {
                Logger.Warn("Client called in background. Not scheduling another connection attempt after 3 tries.");
                return;
            }
            Logger.Warn("Could not connect client. Will retry in 5 seconds.");

            //Retry in 5 seconds
            Task.Delay(5000).ContinueWith(t => reloadConnection(isBackgroundCall, attempt));
        }

        private async Task checkConnectionOnError(Exception e = null) {
            if (clientStatus > STATUS.OFFLINE) {
                if (clientStatus == STATUS.AUTHORISED && (!client.Auth.IsAuthorized || e is TgNotAuthenticatedException)) {
                    //Something went very wrong - set offline as a recovery solution
                    Logger.Warn("Status set to authorised but user is not authorised. Force setting offline status.");
                    clientStatus = STATUS.OFFLINE;
                    await reloadConnection();
                } else {
                    Logger.Warn(e, "Client is possibly disconnected. Force setting offline status.");
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

            string token = DataService.getConfigValue(DataService.DATA_KEYS.FCM_TOKEN, "");

            if (token.Length > 0) {
                Functions.Account.UnregisterDevice unregisterRequest = new Functions.Account.UnregisterDevice(
                        tokenType: 2,
                        token: token,
                        new LanguageExt.Arr<int>()
                    );

                try {
                    await client.Call(unregisterRequest); //https://core.telegram.org/method/account.unregisterDevice
                } catch (Exception e) {
                    Logger.Error(e, "Exception while trying to unregister device.");
                }
            }

            try {
                await client.Call(new Functions.Auth.LogOut()); //https://core.telegram.org/method/auth.logOut
            } catch (Exception e) {
                Logger.Error(e, "Exception while trying to logout user.");
            }

            clientStatus = STATUS.NEW;
            await forceReloadConnection();
        }

        public async Task subscribePushNotifications(string token, bool isInit = false) {
            if (clientStatus != STATUS.AUTHORISED && !isInit) {
                Logger.Warn("Attempted to subscribe to FCM Messages without authorisation.");
                return;
            }
            if (token == null || token.Length < 1) {
                Logger.Warn("Token invalid. Not (re-)registering push notifications.");
                return;
            }

            byte[] aesSecret = new byte[256];
            new Random().NextBytes(aesSecret);

            byte[] aesSecretHash = new SHA1Managed().ComputeHash(aesSecret);
            byte[] aesID = aesSecretHash[(aesSecretHash.Length - 8)..]; //last 8 bytes of hash are ID

            DataService.setConfigValue(DataService.DATA_KEYS.AES_AUTH_KEY, aesSecret);
            DataService.setConfigValue(DataService.DATA_KEYS.AES_AUTH_KEY_ID, aesID);

            Functions.Account.RegisterDevice request = new Functions.Account.RegisterDevice(
                    noMuted: false,
                    tokenType: 2,
                    token: token,
                    appSandbox: false,
                    secret: Telega.Rpc.Dto.BytesExtensions.ToBytes(aesSecret),
                    otherUids: new LanguageExt.Arr<int>());  //https://core.telegram.org/method/account.registerDevice

            try {
                await client.Call(request);
            } catch (Exception e) {
                Logger.Error(e, "Subscribing to push notifications failed");
                await checkConnectionOnError(e);
            }

        }

        public async Task<TStatus> loginWithPassword(string password) {
            if (clientStatus != STATUS.WAIT_PASSWORD) {
                Logger.Warn("Attempted to perform 2FA without appropriate client status. Current status: " + clientStatus.ToString());
                if (clientStatus == STATUS.OFFLINE) {
                    return TStatus.OFFLINE;
                }
                return TStatus.WRONG_CLIENT_STATUS;
            }

            Types.User user;
            try {
                user = await client.Auth.CheckPassword(password);
            } catch (System.InvalidOperationException e) {
                Logger.Warn(e, "Exception trying to confirm password. Presumably the client went offline.");
                await checkConnectionOnError(e);
                return TStatus.OFFLINE;
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

        public async Task<TStatus> requestCode(string phoneNumber, int attempt = 0) {
            if (clientStatus != STATUS.WAIT_PHONE && clientStatus != STATUS.WAIT_CODE && clientStatus != STATUS.WAIT_PASSWORD) {
                Logger.Warn("Attempted to register phone number without appropriate client status. Current status: " + clientStatus.ToString());
                if (clientStatus == STATUS.OFFLINE) {
                    return TStatus.OFFLINE;
                } else {
                    return TStatus.WRONG_CLIENT_STATUS;
                }
            }

            clientPhoneNumber = phoneNumber;

            string hash;
            try {
                hash = await client.Auth.SendCode(KeyService.checkHash(this), clientPhoneNumber);
            } catch (System.InvalidOperationException e) {
                Logger.Warn(e, "Exception trying to authenticate user. Presumably the client went offline.");
                await checkConnectionOnError(e);
                if (clientStatus > STATUS.ONLINE && attempt < 3) {
                    Logger.Info("Connection was possibly fixed. Retrying code request.");
                    return await requestCode(phoneNumber, ++attempt);
                } else {
                    Logger.Warn("Finally failed to request code.");
                    return TStatus.UNKNOWN;
                }
            } catch (System.IO.IOException e) {
                Logger.Warn(e, "Exception trying to authenticate user. Presumably the client went offline.");
                await checkConnectionOnError(e);
                if (clientStatus > STATUS.ONLINE && attempt < 3) {
                    Logger.Info("Connection was possibly fixed. Retrying code request.");
                    return await requestCode(phoneNumber, ++attempt);
                } else {
                    Logger.Warn("Finally failed to request code.");
                    return TStatus.UNKNOWN;
                }

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
                if (clientStatus == STATUS.OFFLINE) {
                    return TStatus.OFFLINE;
                }
                return TStatus.WRONG_CLIENT_STATUS;
            }

            Types.User user;
            try {
                user = await client.Auth.SignIn(clientPhoneNumber, clientRequestCodeHash, code);
            } catch (TgPasswordNeededException) {
                Logger.Info("Two factor authentication needed.");
                clientStatus = STATUS.WAIT_PASSWORD;
                return TStatus.PASSWORD_REQUIRED;
            } catch (TgInvalidPhoneCodeException) {
                Logger.Info("Incorrect code entered.");
                return TStatus.INVALID_CODE;
            } catch (System.InvalidOperationException e) {
                Logger.Warn(e, "Exception trying to confirm code. Presumably the client went offline.");
                await checkConnectionOnError(e);
                return TStatus.OFFLINE;
            } catch (Exception e) {
                Logger.Error(e, "Authenticating user code failed.");
                await checkConnectionOnError(e);
                return TStatus.UNKNOWN;
            }

            await loginCompleted(user);
            return TStatus.OK;
        }

        private async Task loginCompleted(Types.User user) {
            await saveUserData(user);
            clientStatus = STATUS.AUTHORISED;

            string token = DataService.getConfigValue(DataService.DATA_KEYS.FCM_TOKEN, "");
            if (token.Length < 1) {
                Logger.Warn("Could not subscribe to FCM Messages as no token available");
            } else {
                await subscribePushNotifications(token);
            }
        }

        private async Task saveUserData(Types.User user) {
            if (user == null || user.Default == null) {
                Logger.Error("Attempting to save empty user.");
                return;
            }

            Types.User.DefaultTag userTag = user.Default;

            bool hasPhoto = userTag.Photo.IsSome;
            if (hasPhoto) {

                Types.FileLocation profilePhoto = userTag.Photo.Single().Default.PhotoBig;
                Types.InputFileLocation fileLocation = new Types.InputFileLocation.PeerPhotoTag(true, new Types.InputPeer.SelfTag(), profilePhoto.VolumeId, profilePhoto.LocalId);

                MemoryStream file = await getProfilePic(fileLocation);
                if (file != null) {
                    DataService.saveProfilePic(DataService.DATA_KEYS.USER_PHOTO.ToString(), file);
                } else {
                    Logger.Warn("Could not load profile pic.");
                }
            }
            DataService.setConfigValue(DataService.DATA_KEYS.USER_HAS_PHOTO, hasPhoto);

            string userName = userTag.FirstName.IfNone("") + " " + userTag.LastName.IfNone("");
            if (userName.Length < 3) {
                userName = userTag.Username.IfNone("Empty Username");
            }
            string userPhone = "+" + userTag.Phone.IfNone("");

            DataService.setConfigValue(DataService.DATA_KEYS.USER_NAME, userName);
            DataService.setConfigValue(DataService.DATA_KEYS.USER_PHONE, userPhone);

            MessagingCenter.Send(this, MESSAGING_KEYS.USER_DATA_CHANGED.ToString());
        }

        public async Task<Types.Messages.Dialogs> getChatList(int attempt = 0) {
            if (clientStatus != STATUS.AUTHORISED) {
                Logger.Warn("Attempted to load chat list without appropriate client status. Current status: " + clientStatus.ToString());
                return null;
            }

            Types.Messages.Dialogs dialogs;
            try {
                dialogs = await client.Messages.GetDialogs();
            } catch (Exception e) {
                Logger.Error(e, "Exception while trying to fetch chat list.");
                await checkConnectionOnError(e);
                if (clientStatus == STATUS.AUTHORISED && attempt < 3) {
                    Logger.Info("Connection was possibly fixed. Retrying chat retrieval.");
                    return await getChatList(++attempt);
                } else {
                    Logger.Warn("Finally failed to get chat messages. Returning empty list.");
                }
                return null;
            }

            return dialogs;
        }

        public async Task<MemoryStream> getProfilePic(Types.InputFileLocation photo) {
            if (clientStatus < STATUS.ONLINE) {
                Logger.Warn("Attempted to load profile pic without appropriate client status. Current status: " + clientStatus.ToString());
                return null;
            }

            MemoryStream fileStream = new MemoryStream();
            try {
                await client.Upload.DownloadFile(fileStream, photo);
            } catch (TgFloodException) {
                //FloodPrevention is regularly triggered for highly frequented profiles (Telegram, BotFather...)
                Logger.Info("Flood prevention triggered trying to retrieve profile pic.");
                return null;
            } catch (Exception exception) {
                Logger.Error(exception, "Exception while trying to fetch profile pic.");
                await checkConnectionOnError(exception);
                return null;
            }
            return fileStream;
        }

        private async Task<Types.User> getUserUpdate() {
            if (clientStatus < STATUS.ONLINE) {
                Logger.Warn("Attempted to retrieve user update without appropriate client status. Current status: " + clientStatus.ToString());
                return null;
            }

            Types.User outUser;
            try {
                Types.InputUser inUser = new Types.InputUser.SelfTag();
                Arr<Types.InputUser> inArr = new Arr<Types.InputUser>();

                Arr<Types.User> outList = await client.Call(new Functions.Users.GetUsers(inArr.Add(inUser))); //https://core.telegram.org/method/users.getUsers
                outUser = outList.First();
            } catch (Exception e) {
                Logger.Error(e, "Exception while fetching user data.");
                await checkConnectionOnError(e);
                return null;
            }

            return outUser;
        }
    }

    public class MySessionStore : ISessionStore {

        public static string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "CommunicationServiceSession");

        public static void Clear() {
            if (File.Exists(file)) {
                File.Delete(file);
            }
        }

        public async Task<Option<Session>> Load() {
            if (!File.Exists(file)) {
                return default(Session);

            }

            using FileStream fileStream = new FileStream(file, FileMode.Open);
            return await Task.Run<Session>(() => {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return Session.Deserialize(binaryReader);
            });
        }

        public async Task Save(Some<Session> someSession) {
            if (someSession.IsNone) {
                return;
            }


            using FileStream fileStream = new FileStream(file, FileMode.OpenOrCreate);
            await Task.Run(() => {
                BinaryWriter binaryWriter = new BinaryWriter(fileStream);
                someSession.Single().Serialize(binaryWriter);
            });
        }
    }
}
