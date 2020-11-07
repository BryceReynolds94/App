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
using TeleSharp.TL.Upload;
using TeleSharp.TL.Users;
using TLSharp.Core;
using Xamarin.Forms;
using static AlarmManagerT.Services.ClientExceptions;

namespace AlarmManagerT.Services
{
    public class MyClient
    {
        private TelegramClient client;
        private TLUser user;
        private STATUS clientStatus;

        private static readonly int API_ID = ***REMOVED***;
        private static readonly string API_HASH = "***REMOVED***";
        //TODO: Hide these values from potential attackers

        private string clientRequestCodeHash = null;
        private string clientPhoneNumber = null;

        public enum STATUS {NEW, OFFLINE, WAIT_PHONE, WAIT_CODE, AUTHORISED};

        public MyClient()
        {
            //TODO: Implement reconnection
            connectClient();

            //subscribe to token changes
            CrossFirebasePushNotification.Current.OnTokenRefresh += (s, args) =>
            {
                //TODO: Replace logging
                System.Diagnostics.Debug.WriteLine($"TOKEN : {args.Token}");
                subscribePushNotifications(args.Token);
            };
        }

        public STATUS getClientStatus()
        {
            return clientStatus;
        }

        private void changeStatus(STATUS newStatus)
        {
            clientStatus = newStatus;

            StatusChanged?.Invoke(this, null);
        }

        public event EventHandler StatusChanged;

        public async Task<bool> connectClient()
        {
            try
            {
                client = new TelegramClient(API_ID, API_HASH, new MySessionStore(this));
            }
            catch
            {
                //TODO: Log that we have no internet
                changeStatus(STATUS.OFFLINE);
                return false;
            }
            changeStatus(STATUS.OFFLINE);

            //TODO: Implement connection status control
            await client.ConnectAsync();

            changeStatus(STATUS.NEW);

            if (client.IsUserAuthorized())
            {
                changeStatus(STATUS.AUTHORISED);
                saveUserData(await getUser());

                //TODO: Require some wait or retry here?

                string token = await CrossFirebasePushNotification.Current.GetTokenAsync();
                await subscribePushNotifications(token); //TODO: Only do this on login?
            }
            else
            {
                changeStatus(STATUS.WAIT_PHONE);
            }
            return true;
        }

        public async Task subscribePushNotifications(string token)
        {
            if(clientStatus != STATUS.AUTHORISED)
            {
                //TODO: handle this
                return;
            }

            if(token.Length < 1)
            {
                //Token invalid
                return;
            }

            TLRequestRegisterDevice request = new TLRequestRegisterDevice()
            {
                TokenType = 2, //2 = FCM, use  for APNs
                Token = token //TODO: See wether we have to check this is valid
            };

            try
            {
                await client.SendRequestAsync<bool>(request);
            }catch(Exception e)
            {
                return;
            }


            return;
        }

        public async Task<TStatus> requestCode(string phoneNumber)
        {
            if(clientStatus != STATUS.WAIT_PHONE)
            {
                //TODO: Handle Error - wrong status
                Console.WriteLine("Wrong status");
                return TStatus.WRONG_CLIENT_STATUS;
            }

            clientPhoneNumber = phoneNumber;

            //TODO: check if user is registered first
            //await client.IsPhoneRegisteredAsync(phoneNumber);

            TLMethod requestCode = new TeleSharp.TL.Auth.TLRequestSendCode()
            {
                PhoneNumber = clientPhoneNumber,
                ApiId = API_ID,
                ApiHash = API_HASH
            };

            TeleSharp.TL.Auth.TLSentCode code;
            try
            {
                code = await client.SendRequestAsync<TeleSharp.TL.Auth.TLSentCode>(requestCode);
            }catch(Exception e)
            {

                TException exception = getTException(e.Message);
                switch (exception)
                {
                    case TException.API_ID_INVALID:
                    case TException.API_ID_PUBLISHED_FLOOD:
                        //TODO: Handle this fatal stuff
                        break;
                    case TException.NETWORK_MIGRATE_X:
                    case TException.PHONE_MIGRATE_X:
                        //TODO: Handle this. Should not really ever occur - test in debugging
                        break;
                    case TException.PHONE_NUMBER_BANNED:
                    case TException.PHONE_NUMBER_FLOOD:
                    case TException.PHONE_NUMBER_INVALID:
                    case TException.PHONE_PASSWORD_FLOOD:
                        return TStatus.INVALID_PHONE_NUMBER;
                        //TODO: Inform user that phone number is invalid/blocked
                    case TException.PHONE_PASSWORD_PROTECTED:
                        //TODO: Implement 2FA for password protected accounts
                        break;

                    case TException.AUTH_RESTART:
                        //TODO: Handle relogin scenario
                        break;
                    default:
                        //Some other fatal problem - warn and quit
                        //TODO: Handle this
                        break;
                }

                return TStatus.UNKNOWN;
            }
            clientRequestCodeHash = code.PhoneCodeHash;
            changeStatus(STATUS.WAIT_CODE);
            return TStatus.OK;
        }

        public async Task<TStatus> confirmCode(string code)
        {
            if(clientStatus != STATUS.WAIT_CODE)
            {
                //TODO: Handle error - wrong status
                return TStatus.WRONG_CLIENT_STATUS;
            }

            try
            {
                user = await client.MakeAuthAsync(clientPhoneNumber, clientRequestCodeHash, code);
            }catch(Exception e)
            {
                //TODO: Handle exceptions
                return TStatus.UNKNOWN;
            }
            saveUserData(user);
            changeStatus(STATUS.AUTHORISED);
            subscribePushNotifications(CrossFirebasePushNotification.Current.Token);
            return TStatus.OK;
        }

        public async void saveUserData(TLUser user)
        {
            TLUserProfilePhoto photo = (user.Photo as TLUserProfilePhoto);
            bool hasPhoto = false;
            if (photo != null)
            {
                TLFile file = await getProfilePic(photo.PhotoBig as TLFileLocation);
                MemoryStream memoryStream = new MemoryStream(file.Bytes);

                Data.saveProfilePic(Data.DATA_KEYS.USER_PHOTO.ToString(), memoryStream);
                hasPhoto = true;
            }
            Data.setConfigValue(Data.DATA_KEYS.USER_HAS_PHOTO, hasPhoto);

            string userName = user.FirstName + " " + user.LastName;
            if (userName.Length < 3)
            {
                userName = user.Username;
            }
            string userPhone = "+" +user.Phone;

            Data.setConfigValue(Data.DATA_KEYS.USER_NAME, userName);
            Data.setConfigValue(Data.DATA_KEYS.USER_PHONE, userPhone);

            MessagingCenter.Send(this, "UserDataChanged");
        }

        public async Task<TLVector<TLAbsChat>> getChatList()
        {
            if(clientStatus != STATUS.AUTHORISED)
            {
                //TODO: Handle error
            }

            TLMethod requestDialogList = new TeleSharp.TL.Messages.TLRequestGetDialogs() {
                OffsetDate = 0,
                OffsetId = 0,
                OffsetPeer = new TLInputPeerSelf(),
                Limit = 100
            
            };

            if (!client.IsUserAuthorized())
            {
                //we have a Problem
                return null;
            }

            TLDialogs dialogs;
            try
            {
                dialogs = await client.SendRequestAsync<TLDialogs>(requestDialogList);
            }catch(Exception e)
            {
                return null;
            }

            return dialogs.Chats;
        }

        public async Task<TLFile> getProfilePic(TLFileLocation location)
        {
            TLInputFileLocation loc = new TLInputFileLocation()
            {
                LocalId = location.LocalId,
                Secret = location.Secret,
                VolumeId = location.VolumeId
            };

            TLFile file = await client.GetFile(loc, 1024*256);
            return file;
        }

        public async Task<TLFile> getProfilePic(int chatID)
        {
            TLRequestGetChats request = new TLRequestGetChats()
            {
                Id = new TLVector<int>() { chatID }
            };

            TLVector<TLChat> foundChats = await client.SendRequestAsync<TLVector<TLChat>>(request);

            TLFileLocation loc = (foundChats.First().Photo as TLChatPhoto).PhotoSmall as TLFileLocation;

            TLFile file = await getProfilePic(loc);

            return file;
        }

        public async Task<TLUser> getUser()
        {

            TLRequestGetUsers request = new TLRequestGetUsers()
            {
                Id = new TLVector<TLAbsInputUser> { new TLInputUser() { UserId = user.Id, AccessHash = (long)user.AccessHash } }
            };


            TLVector<TLAbsUser> outUser;
            try
            {
                outUser = await client.SendRequestAsync<TLVector<TLAbsUser>>(request);
                user = outUser.First() as TLUser;
            }catch(Exception e)
            {
                return null;
            }

            return user;
        }

        public class MySessionStore : ISessionStore
        {

            private MyClient client;
            public MySessionStore(MyClient client)
            {
                this.client = client;
            }

            public static string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "session");
            public void Save(Session session)
            {

                using (FileStream fileStream = new FileStream(string.Format(file, (object)session.SessionUserId), FileMode.OpenOrCreate))
                {
                    byte[] bytes = session.ToBytes();
                    fileStream.Write(bytes, 0, bytes.Length);
                }
            }

            public Session Load(string sessionUserId)
            {

                string path = string.Format(file, (object)sessionUserId);
                if (!File.Exists(path))
                    return (Session)null;

                var buffer = File.ReadAllBytes(path);
                Session session =  Session.FromBytes(buffer, this, sessionUserId);
                client.user = session.TLUser;
                return session;
            }
        }


    }
}
