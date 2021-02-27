using Newtonsoft.Json.Linq;
using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PagerBuddy.Services
{
    public class MessagingService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private CommunicationService client;
        private static MessagingService instance;

        public MessagingService(CommunicationService client) {
            this.client = client;
        }


        //This is called when FCM Messages are received after app is killed
        public static void FirebaseMessage(object sender, IDictionary<string,string> data, long timestamp)
        {
            Logger.Debug("A firebase message was received.");
            inspectPayload(data, timestamp);
        }

        public static async Task FirebaseTokenRefresh(object sender, string token) {
            Logger.Info("Firebase token was updated, TOKEN: {0}", token);
            DataService.setConfigValue(DataService.DATA_KEYS.FCM_TOKEN, token);

            if (instance != null) {
                if (instance.client.clientStatus == CommunicationService.STATUS.AUTHORISED) {
                    await instance.client.subscribePushNotifications(token);
                }
            } else {
                CommunicationService client = new CommunicationService(true);
                client.StatusChanged += async (sender, status) => {
                    if (status == CommunicationService.STATUS.AUTHORISED) {
                        await client.subscribePushNotifications(token);
                    }
                };
            }  
        }

        private static void inspectPayload(IDictionary<string,string> data, long timestamp) {
            //https://github.com/DrKLO/Telegram/blob/master/TMessagesProj/src/main/java/org/telegram/messenger/GcmPushListenerService.java

            string rawPayload;
            bool success = data.TryGetValue("p", out rawPayload);

            if(!success) {
                Logger.Warn("FCM message did not contain payload.");
                return;
            }

            //Payload is bas64 URL encoded - we have to get standard base64
            string stringPayload = rawPayload.Replace("-", "+").Replace("_", "/");
            switch(stringPayload.Length % 4) {
                case 2:
                    stringPayload += "==";
                    break;
                case 3:
                    stringPayload += "=";
                    break;
            }

            byte[] bytes;
            try {
                bytes = Convert.FromBase64String(stringPayload);
            }catch(Exception e) {
                Logger.Error(e, "Exception trying to decode Base64 payload.");
                return;
            }

            MemoryStream stream = new MemoryStream(bytes);

            byte[] authKey = DataService.getConfigValue(DataService.DATA_KEYS.AES_AUTH_KEY, new byte[0]);
            byte[] authKeyID = DataService.getConfigValue(DataService.DATA_KEYS.AES_AUTH_KEY_ID, new byte[0]);
            if (authKey.Length < 1 || authKeyID.Length < 8) {
                Logger.Info("Auth key and ID not set. Ignoring FCM message as we cannot decode it.");
                return;
            }

            Span<byte> payloadAuthKeyID = new Span<byte>(new byte[8]);
            stream.Read(payloadAuthKeyID);

            if(!Enumerable.SequenceEqual(authKeyID, payloadAuthKeyID.ToArray())) {
                Logger.Warn("The FCM payload auth key ID did not match. Ignoring package.");
                return;
            }

            Span<byte> messageKey = new Span<byte>(new byte[16]);
            stream.Read(messageKey);

            (byte[] aesKey, byte[] aesIv) = getKeyData(authKey, messageKey.ToArray());
            byte[] decrypted = AESIGEDecrypt(bytes[24..], aesKey, aesIv); //start decryption at byte 24

            int length = BitConverter.ToInt32(decrypted, 0); //first 4 bytes contain number representing message length
            byte[] strBytes = decrypted[4..(length + 4)];

            JObject json;
            try {
                json = JObject.Parse(Encoding.UTF8.GetString(strBytes));
            }catch(Exception e) {
                Logger.Error(e, "Exception parsing the FCM payload.");
                return;
            }

            handleUpdate(json);
        }

        private static void handleUpdate(JObject jsonPayload) {

            string loc_key;
            if (jsonPayload.ContainsKey("loc_key")) {
                loc_key = (string) jsonPayload.GetValue("loc_key");
                Logger.Debug("Update is of type " + loc_key);
            } else {
                Logger.Info("Payload does not contain loc_key. Will not further process update.");
                return;
            }

            string message = "";
            if (jsonPayload.ContainsKey("loc_args")) {
                IEnumerable<JToken> token = jsonPayload.GetValue("loc_args").Children();
                Logger.Debug("Payload contains " + token.Length() + " loc_args.");

                if(token.Length() > 1) {
                    message = (string) token.ElementAt(1);
                }
            }

            int senderID;
            if (jsonPayload.ContainsKey("custom")) {
                JObject custom = (JObject) jsonPayload.GetValue("custom");

                string rawID;
                if (custom.ContainsKey("channel_id")) {
                    rawID = (string) custom.GetValue("channel_id");
                    senderID = -int.Parse(rawID);
                }else if (custom.ContainsKey("chat_id")) {
                    rawID = (string) custom.GetValue("chat_id");
                    senderID = -int.Parse(rawID);
                }else if (custom.ContainsKey("from_id")) {
                    rawID = (string) custom.GetValue("from_id");
                    senderID = int.Parse(rawID);
                } else {
                    Logger.Info("Could not get sender id from payload. Not processing update further.");
                    return;
                }
            } else {
                Logger.Info("Payload does not contain 'custom' key. Will not process update further.");
                return;
            }

            switch (loc_key) {
                case "CHAT_TITLE_EDITED":
                    //TODO: Implement title update. This should set title value of associated config.
                    return;

                case "CHAT_PHOTO_EDITED":
                    //TODO: Implement photo update. Reload chat photo and persist.
                    return;

                case "CHAT_DELETE_YOU":
                    //TODO: Think about handling. Possibly delete config all together.
                    return;

                case "MESSAGE_TEXT":
                case "MESSAGE_NOTEXT":
                case "MESSAGE_PHOTO":
                case "MESSAGE_PHOTO_SECRET":
                case "MESSAGE_VIDEO":
                case "MESSAGE_VIDEO_SECRET":
                case "MESSAGE_SCREENSHOT":
                case "MESSAGE_ROUND":
                case "MESSAGE_DOC":
                case "MESSAGE_STICKER":
                case "MESSAGE_AUDIO":
                case "MESSAGE_CONTACT":
                case "MESSAGE_QUIZ":
                case "MESSAGE_POLL":
                case "MESSAGE_GEO":
                case "MESSAGE_GEOLIVE":
                case "MESSAGE_GIF":
                case "MESSAGE_GAME":
                case "MESSAGE_GAME_SCORE":
                case "MESSAGE_INVOICE":
                case "MESSAGE_FWDS":
                case "MESSAGE_PHOTOS":
                case "MESSAGE_VIDEOS":
                case "MESSAGE_PLAYLIST":
                case "MESSAGE_DOCS":
                case "MESSAGE_MUTED":
                case "MESSAGES":

                case "CHANNEL_MESSAGE_TEXT":
                case "CHANNEL_MESSAGE_NOTEXT":
                case "CHANNEL_MESSAGE_GAME_SCORE":
                case "CHANNEL_MESSAGE_PHOTO":
                case "CHANNEL_MESSAGE_VIDEO":
                case "CHANNEL_MESSAGE_ROUND":
                case "CHANNEL_MESSAGE_DOC":
                case "CHANNEL_MESSAGE_STICKER":
                case "CHANNEL_MESSAGE_AUDIO":
                case "CHANNEL_MESSAGE_CONTACT":
                case "CHANNEL_MESSAGE_QUIZ":
                case "CHANNEL_MESSAGE_POLL":
                case "CHANNEL_MESSAGE_GEO":
                case "CHANNEL_MESSAGE_GEOLIVE":
                case "CHANNEL_MESSAGE_GIF":
                case "CHANNEL_MESSAGE_GAME":
                case "CHANNEL_MESSAGE_FWDS":
                case "CHANNEL_MESSAGE_PHOTOS":
                case "CHANNEL_MESSAGE_VIDEOS":
                case "CHANNEL_MESSAGE_PLAYLIST":
                case "CHANNEL_MESSAGE_DOCS":
                case "CHANNEL_MESSAGES":

                case "CHAT_MESSAGE_TEXT":
                case "CHAT_MESSAGE_NOTEXT":
                case "CHAT_MESSAGE_PHOTO":
                case "CHAT_MESSAGE_VIDEO":
                case "CHAT_MESSAGE_ROUND":
                case "CHAT_MESSAGE_DOC":
                case "CHAT_MESSAGE_STICKER":
                case "CHAT_MESSAGE_AUDIO":
                case "CHAT_MESSAGE_CONTACT":
                case "CHAT_MESSAGE_QUIZ":
                case "CHAT_MESSAGE_POLL":
                case "CHAT_MESSAGE_GEO":
                case "CHAT_MESSAGE_GEOLIVE":
                case "CHAT_MESSAGE_GIF":
                case "CHAT_MESSAGE_GAME":
                case "CHAT_MESSAGE_GAME_SCORE":
                case "CHAT_MESSAGE_INVOICE":
                case "CHAT_MESSAGE_FWDS":
                case "CHAT_MESSAGE_PHOTOS":
                case "CHAT_MESSAGE_VIDEOS":
                case "CHAT_MESSAGE_PLAYLIST":
                case "CHAT_MESSAGE_DOCS":
                case "CHAT_MESSAGES":

                case "ENCRYPTED_MESSAGE":
                    //TODO: RBF
                    new AlertService(message, senderID, 0);
                    break;


                case "READ_HISTORY":
                case "MESSAGE_DELETED":
                case "CHAT_ADD_MEMBER":
                case "CHAT_VOICECHAT_START":
                case "CHAT_VOICECHAT_INVITE":
                case "CHAT_VOICECHAT_END":
                case "CHAT_VOICECHAT_INVITE_YOU":
                case "CHAT_DELETE_MEMBER":
                case "CHAT_CREATED":
                case "CHAT_ADD_YOU":
                case "CHAT_LEFT":
                case "CHAT_RETURNED":
                case "CHAT_JOINED":
                case "PINNED_TEXT":
                case "PINNED_NOTEXT": 
                case "PINNED_PHOTO": 
                case "PINNED_VIDEO":
                case "PINNED_ROUND":
                case "PINNED_DOC":
                case "PINNED_STICKER":
                case "PINNED_AUDIO":
                case "PINNED_CONTACT":
                case "PINNED_QUIZ":
                case "PINNED_POLL":
                case "PINNED_GEO":
                case "PINNED_GEOLIVE":
                case "PINNED_GAME":
                case "PINNED_GAME_SCORE":
                case "PINNED_INVOICE":
                case "PINNED_GIF":
                case "CONTACT_JOINED":
                case "AUTH_UNKNOWN":
                case "AUTH_REGION":
                case "LOCKED_MESSAGE":
                case "ENCRYPTION_REQUEST":
                case "ENCRYPTION_ACCEPT":
                case "PHONE_CALL_REQUEST":
                case "PHONE_CALL_MISSED":
                default:
                    return;

                    }
        }

        private static (byte[] aesKey, byte[] aesIv) getKeyData(byte[] authKey, byte[] messageKey) {
            //https://github.com/DrKLO/Telegram/blob/3480f19272fbe7679172dc51473e19fcf184501c/TMessagesProj/src/main/java/org/telegram/messenger/MessageKeyData.java#L18
            MemoryStream streamA = new MemoryStream();
            streamA.Write(messageKey, 0, 16);
            streamA.Write(authKey, 8, 36);
            byte[] sha256_a = new SHA256Managed().ComputeHash(streamA.ToArray());
            streamA.Dispose();

            MemoryStream streamB = new MemoryStream();
            streamB.Write(authKey, 48, 36);
            streamB.Write(messageKey, 0, 16);
            byte[] sha256_b = new SHA256Managed().ComputeHash(streamB.ToArray());
            streamB.Dispose();

            MemoryStream streamKey = new MemoryStream();
            streamKey.Write(sha256_a, 0, 8);
            streamKey.Write(sha256_b, 8, 16);
            streamKey.Write(sha256_a, 24, 8);
            byte[] aesKey = streamKey.ToArray();
            streamKey.Dispose();

            MemoryStream streamIv = new MemoryStream();
            streamIv.Write(sha256_b, 0, 8);
            streamIv.Write(sha256_a, 8, 16);
            streamIv.Write(sha256_b, 24, 8);
            byte[] aesIv = streamIv.ToArray();
            streamIv.Dispose();

            return (aesKey, aesIv);
        }

        private static byte[] AESIGEDecrypt(Span<byte> bytes, byte[] key, byte[] iv) {
            //https://mgp25.com/AESIGE/
            //https://stackoverflow.com/questions/58996069/aes-igeinfinite-garble-extension-mode-in-net-core
            AesManaged aes = new AesManaged {
                Mode = CipherMode.ECB,
                Padding = PaddingMode.None
            };

            int size = aes.BlockSize / 8;
            ICryptoTransform decryptor = aes.CreateDecryptor(key, null);

            byte[] c = iv[size..];
            byte[] m = iv[..size];

            byte[] decryptedData = new byte[bytes.Length];

            byte[] outTemp = new byte[size];
            for (int i = 0; i < (bytes.Length - size); i += size) {
                Span<byte> block = bytes[i..(i + size)];
                decryptor.TransformBlock(XOR(c, block), 0, block.Length, outTemp, 0);
                c = XOR(m, outTemp);

                c.CopyTo(decryptedData, i);
                m = block.ToArray();
            }
            return decryptedData;
        }

        private static byte[] XOR(Span<byte> a, Span<byte> b) {
            byte[] c = new byte[a.Length];
            for (int i = 0; i < a.Length; i++) {
                c[i] = (byte)(a[i] ^ b[i]);
            }
            return c;
        }

    }
}
