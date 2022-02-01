using PagerBuddy.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using static PagerBuddy.Services.ClientExceptions;
using Telega;
using Telega.Client;
using Functions = Telega.Rpc.Dto.Functions;
using Types = Telega.Rpc.Dto.Types;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Xamarin.Essentials;
using PagerBuddy.Interfaces;

namespace PagerBuddy.Services {

    public class CommunicationService {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private TelegramClient client;
        private STATUS status;

        private static readonly List<string> STATIC_PAGERBUDDY_SERVER_BOTS = new List<string> { "pagerbuddyserverbot" };

        public static List<string> pagerbuddyServerList {
            get {
                List<string> baseList = STATIC_PAGERBUDDY_SERVER_BOTS;
#if DEBUG
                baseList = new List<string> { "pagerbuddyservertestbot" }; //Ignore real bots when testing
#endif
                baseList.AddRange(DataService.customPagerBuddyServerBots);
                return baseList;
            }
        }

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

        public enum STATUS {LOGOUT, NEW, OFFLINE, ONLINE, WAIT_PHONE, WAIT_CODE, WAIT_PASSWORD, AUTHORISED };

        public enum MESSAGING_KEYS { USER_DATA_CHANGED };

        public CommunicationService() {
            Logger.Debug("Initialising CommunicationService.");
        }

        public async Task reloadConnection(bool isBackgroundCall = false) {
            if (clientStatus == STATUS.OFFLINE) { //Do not reload if we are currently connecting or have successfully connected
                Logger.Info("Reloading connection.");
                await connectClient(isBackgroundCall);
            }
        }

        public async Task forceReloadConnection(bool isBackgroundCall = false) {
            if (client != null) {
                client.Dispose();
                client = null;
            }
            MySessionStore.Clear();

            await connectClient(isBackgroundCall);
        }

        public event ClientStausEventHandler StatusChanged;
        public delegate void ClientStausEventHandler(object sender, STATUS newStatus);

        public async Task connectClient(bool isBackgroundCall = false) {
            Logger.Debug("Connecting communication service. Is background call: " + isBackgroundCall);

            clientStatus = STATUS.NEW;
            if (Connectivity.NetworkAccess != NetworkAccess.Internet) {
                Logger.Info("Not connected to internet. Cannot initialise client.");
                clientStatus = STATUS.OFFLINE;
                scheduleRetry(isBackgroundCall);
            }

            try {
                client = await TelegramClient.Connect(KeyService.checkID(this), store: new MySessionStore());
            } catch (Exception e) {
                Logger.Error(e, "Initialisation of TelegramClient failed");

                if (e is TgException && e.Message.Contains("Invalid session file")) { //Handle corrupt session files. (Trautner Bug)
                    Logger.Warn("Session file was corrupted. Clearing session file.");
                    MySessionStore.Clear();
                }

                clientStatus = STATUS.OFFLINE;
                scheduleRetry(isBackgroundCall);
                return;
            }

            clientStatus = STATUS.ONLINE;

            if (client.Auth.IsAuthorized) {
                Logger.Debug("User is authorised.");
                if (!isBackgroundCall) { //only do this stuff if we are not running in background
                    Types.User user = await getUser(new Types.InputUser.SelfTag());
                    if (user != null) {
                        await saveUserData(user);
                    }
                }
                if (clientStatus != STATUS.ONLINE) { //status may have changed out of scope due to previous call fallbacks
                    Logger.Info("Connect process completed, but client Status was changed out of scope. Not setting authorised status. CurrentStatus: " + clientStatus.ToString());
                    return;
                }
                clientStatus = STATUS.AUTHORISED;
            } else {
                Logger.Debug("User is not authorised. Awaiting login data.");
                clientStatus = STATUS.WAIT_PHONE;
            }
        }

        private void scheduleRetry(bool isBackgroundCall) {
            if (isBackgroundCall) {
                Logger.Warn("Client called in background. Not retrying.");
                return;
            }

            if (Connectivity.NetworkAccess != NetworkAccess.Internet) {
                Logger.Info("Subscribing to network status changes for retry attempt.");

                async void handler(object sender, ConnectivityChangedEventArgs e) {
                    Logger.Debug("Network status changed to " + e.NetworkAccess);
                    if (e.NetworkAccess == NetworkAccess.Internet) {
                        Logger.Info("Internet connection established. Retrying connection.");
                        Connectivity.ConnectivityChanged -= handler; //unsubscribe self to clean up
                        await reloadConnection(isBackgroundCall);
                    }
                }

                Connectivity.ConnectivityChanged += handler;
                return;
            }

            Logger.Warn("Could not connect client. Will retry in 5 seconds.");
            //Retry in 5 seconds
            Task.Delay(5000).ContinueWith(t => reloadConnection(isBackgroundCall));
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
            clientStatus = STATUS.LOGOUT;

            //Try to unregister user with servers
            foreach(string server in pagerbuddyServerList) {
                Task request = sendServerRequest(new Collection<AlertConfig>(), server);
                Task.WaitAny(Task.Delay(2000), request); //Timeout after 2s if not completed
            }

            try {
                await client.Call(new Functions.Auth.LogOut()); //https://core.telegram.org/method/auth.logOut
            } catch (Exception e) {
                Logger.Error(e, "Exception while trying to logout user.");
            }

            clientStatus = STATUS.NEW;
            await forceReloadConnection();
        }

        public async Task legacyUnregister(string token) {
            //TODO Later: Once all users are migrated this can be removed
            if(Device.RuntimePlatform == Device.Android) {
                Logger.Debug("Attempting to unregister user from legacy messaging.");
                if (token.Length > 0) {
                    Functions.Account.UnregisterDevice unregisterRequest = new Functions.Account.UnregisterDevice(
                            tokenType: 2,
                            token: token,
                            new List<long>()
                        );

                    try {
                        await client.Call(unregisterRequest); //https://core.telegram.org/method/account.unregisterDevice
                    } catch (Exception e) {
                        Logger.Error(e, "Exception while trying to unregister device.");
                    }
                }


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
                TException exception = getTException(e.Message);
                if(exception == TException.PHONE_CODE_EXPIRED) {
                    Logger.Info("Code has expired.");
                    return TStatus.INVALID_CODE;
                }

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
        }

        private async Task saveUserData(Types.User user) {
            if (user == null || user.Default == null) {
                Logger.Error("Attempting to save empty user.");
                return;
            }

            Types.User.DefaultTag userTag = user.Default;

            bool hasPhoto = userTag.Photo?.Default != null;
            if (hasPhoto) {

                long photoID = userTag.Photo.Default.PhotoId;
                Types.InputFileLocation fileLocation = new Types.InputFileLocation.PeerPhotoTag(true, new Types.InputPeer.SelfTag(), photoID);

                MemoryStream file = await getProfilePic(fileLocation);
                if (file != null) {
                    DataService.saveProfilePic(DataService.DATA_KEYS.USER_PHOTO.ToString(), file);
                } else {
                    Logger.Warn("Could not load profile pic.");
                }
            }
            DataService.setConfigValue(DataService.DATA_KEYS.USER_HAS_PHOTO, hasPhoto);

            string userName = userTag.FirstName + " " + userTag.LastName;
            if (userName.Length < 3) {
                userName = userTag.Username;
            }
            string userPhone = "+" + userTag.Phone;

            DataService.setConfigValue(DataService.DATA_KEYS.USER_NAME, userName);
            DataService.setConfigValue(DataService.DATA_KEYS.USER_PHONE, userPhone);

            MessagingCenter.Send(this, MESSAGING_KEYS.USER_DATA_CHANGED.ToString());
        }

        public void mockLogin() {
            DataService.setConfigValue(DataService.DATA_KEYS.USER_HAS_PHOTO, false);
            DataService.setConfigValue(DataService.DATA_KEYS.USER_NAME, "Mock User");
            DataService.setConfigValue(DataService.DATA_KEYS.USER_PHONE, "+12099999999");

            MessagingCenter.Send(this, MESSAGING_KEYS.USER_DATA_CHANGED.ToString());
        }

        public async Task<Types.Messages.Chats> getChatList(string pagerbuddyserver, int attempt = 0) {
            if (clientStatus != STATUS.AUTHORISED) {
                Logger.Warn("Attempted to load chat list without appropriate client status. Current status: " + clientStatus.ToString());
                return null;
            }

            Types.InputUser botUser;
            try {
                Types.Contacts.ResolvedPeer resolvedPeer = await client.Contacts.ResolveUsername(pagerbuddyserver);
                Types.User.DefaultTag resolvedUser = resolvedPeer.Users.First().Default;

                if (resolvedUser != null) {
                    botUser = new Types.InputUser.DefaultTag(resolvedUser.Id, resolvedUser.AccessHash.GetValueOrDefault(0));
                } else {
                    Logger.Info("PagerBuddy-Server peer '" + pagerbuddyserver + "' could not be resolved.");
                    return null;
                }
            } catch (Exception e) {
                Logger.Error(e, "Exception while resolving PagerBuddy-Server username.");
                await checkConnectionOnError(e);
                return null;
            }

            Types.Messages.Chats chats;
            try {
                Functions.Messages.GetCommonChats func = new Functions.Messages.GetCommonChats(botUser, 100, 100); //https://core.telegram.org/method/messages.getCommonChats
                chats = await client.Call(func);
            } catch (Exception e) {
                Logger.Error(e, "Exception while trying to fetch chat list.");
                await checkConnectionOnError(e);
                if (clientStatus == STATUS.AUTHORISED && attempt < 3) {
                    Logger.Info("Connection was possibly fixed. Retrying chat retrieval.");
                    return await getChatList(pagerbuddyserver, ++attempt);
                } else {
                    Logger.Warn("Finally failed to get chat messages. Returning empty list.");
                }
                return null;
            }

            return chats;
        }

        public Types.Messages.Chats getMockChatList() {
            
            Types.Chat chat1 = new Types.Chat.DefaultTag(false, false, false, false, false, false, false, 100000, "Firebrigade", new Types.ChatPhoto.EmptyTag(), 1, 0, 0, null, null, null);
            Types.Chat chat2 = new Types.Chat.DefaultTag(false, false, false, false, false, false, false, 200000, "Annina", new Types.ChatPhoto.EmptyTag(), 1, 0, 0, null, null, null);
            return new Types.Messages.Chats.DefaultTag(new List<Types.Chat>() { chat1, chat2 });
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

        private async Task<Types.User> getUser(Types.InputUser inUser) {
            if (clientStatus < STATUS.ONLINE) {
                Logger.Warn("Attempted to retrieve user without appropriate client status. Current status: " + clientStatus.ToString());
                return null;
            }

            Types.User outUser;
            try {
                List<Types.InputUser> inArr = new List<Types.InputUser> {
                    inUser
                };

                IReadOnlyList<Types.User> outList = await client.Call(new Functions.Users.GetUsers(inArr)); //https://core.telegram.org/method/users.getUsers
                outUser = outList.First();
            } catch (Exception e) {
                Logger.Error(e, "Exception while fetching user data.");
                await checkConnectionOnError(e);
                return null;
            }

            return outUser;
        }

        public async Task<bool> sendServerRequest(Collection<AlertConfig> configList, string serverPeer, int attempt = 0) {
            if (clientStatus < STATUS.AUTHORISED && clientStatus != STATUS.LOGOUT) {
                Logger.Warn("Attempted to send request to server without appropriate client status. Current status: " + clientStatus.ToString());
                return false;
            }

            Types.InputPeer botPeer;
            Types.InputUser botUser;
            try {
                Types.Contacts.ResolvedPeer resolvedPeer = await client.Contacts.ResolveUsername(serverPeer);
                Types.User.DefaultTag resolvedUser = resolvedPeer.Users.First().Default;

                if (resolvedUser != null) {
                    botPeer = new Types.InputPeer.UserTag(resolvedUser.Id, resolvedUser.AccessHash.GetValueOrDefault(0));
                    botUser = new Types.InputUser.DefaultTag(resolvedUser.Id, resolvedUser.AccessHash.GetValueOrDefault(0));
                } else {
                    Logger.Info("PagerBuddy-Server peer could not be resolved.");
                    return false;
                }
            } catch (Exception e) {
                Logger.Error(e, "Exception while resolving PagerBuddy-Server username.");
                await checkConnectionOnError(e);
                if (clientStatus == STATUS.AUTHORISED && attempt < 3) {
                    Logger.Info("Connection was possibly fixed. Retrying to send server request.");
                    return await sendServerRequest(configList, serverPeer, ++attempt);
                } else {
                    Logger.Warn("Finally failed to send server request.");
                    //Retry will be handled by scheduler
                    return false;
                }
            }

            ServerRequest request = ServerRequest.getServerRequest(configList);
            if (request == null) {
                return false;
            }

            string jsonRequest = JsonConvert.SerializeObject(request);
            Logger.Debug("Server request JSON payload: " + jsonRequest);

            string stringRequest = ServerRequest.PREFIX + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonRequest));
            Logger.Debug("Sending update to server: " + stringRequest);

            int msgID = 0;
            try {
                Functions.Messages.SendMessage msg = new Functions.Messages.SendMessage(true, true, true, true, false, botPeer, null, stringRequest, new Random().Next(), null, null, null, null);
                Types.UpdatesType update = await client.Call(msg); //https://core.telegram.org/method/messages.sendMessage

                if(update.Default.Updates.Count > 0) {
                    foreach(Types.Update upd in update.Default.Updates) {
                        if(upd.MessageId != null) {
                            msgID = upd.MessageId.Id;
                            break;
                        }
                    }
                }
            } catch (Exception e) {
                Logger.Error(e, "Exception while sending request to PagerBuddy-Server.");

                TException exception = getTException(e.Message);
                switch (exception) {
                    case TException.YOU_BLOCKED_USER:
                        Logger.Info("Bot is blocked by user. Will unblock and retry.");
                        try {
                            Functions.Contacts.Unblock unblockFunc = new Functions.Contacts.Unblock(botPeer);
                            bool res = await client.Call(unblockFunc);
                        }catch(Exception error) {
                            Logger.Error(error, "Exception while sending unblock command.");
                            break;
                        }
                        return await sendServerRequest(configList, serverPeer, ++attempt);

                    case TException.MESSAGE_TOO_LONG:
                        Logger.Error("Cannot send server update. The message is too long.");
                        return false;
                    case TException.MESSAGE_EMPTY:
                        Logger.Error("Cannpt send server update. The message body is empty.");
                        return false;
                }

                await checkConnectionOnError(e);
                if (clientStatus == STATUS.AUTHORISED && attempt < 3) {
                    Logger.Info("Connection was possibly fixed. Retrying to send server request.");
                    return await sendServerRequest(configList, serverPeer, ++attempt);
                } else {
                    Logger.Warn("Finally failed to send server request.");
                    //Retry will be handled by scheduler
                    return false;
                }
            }

            //From here on we assume sending the message to the server was successfull
            await deleteMessage(msgID);
            return true;
        }

        private async Task deleteMessage(int messageID, int attempt = 0) {
            if (clientStatus < STATUS.AUTHORISED && clientStatus != STATUS.LOGOUT) {
                Logger.Warn("Attempted to delete message without appropriate client status. Current status: " + clientStatus.ToString());
                return;
            }

            try {
                Functions.Messages.DeleteMessages func = new Functions.Messages.DeleteMessages(false, new List<int>() { messageID });
                Types.Messages.AffectedMessages update = await client.Call(func); //https://core.telegram.org/method/messages.deleteMessages

            } catch (Exception e) {
                Logger.Error(e, "Exception while deleting sent message.");
                await checkConnectionOnError(e);
                if (clientStatus == STATUS.AUTHORISED && attempt < 3) {
                    Logger.Info("Connection was possibly fixed. Retrying to send server request.");
                    await deleteMessage(messageID, ++attempt);
                } else {
                    Logger.Warn("Finally failed to delete message.");
                }
                return;
            }
        }
    }

    public class MySessionStore : ISessionStore {

        public static string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "CommunicationServiceSession");

        public static void Clear() {
            if (File.Exists(file)) {
                File.Delete(file);
            }
        }

        public async Task<Session> Load() {
            if (!File.Exists(file)) {
                return default;

            }

            using FileStream fileStream = new FileStream(file, FileMode.Open);
            return await Task.Run<Session>(() => {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return Session.Deserialize(binaryReader);
            });
        }

        public async Task Save(Session someSession) {
            if (someSession == null) {
                return;
            }


            using FileStream fileStream = new FileStream(file, FileMode.OpenOrCreate);
            await Task.Run(() => {
                BinaryWriter binaryWriter = new BinaryWriter(fileStream);
                someSession.Serialize(binaryWriter);
            });
        }
    }
}
