using Newtonsoft.Json.Linq;
using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using Plugin.FirebasePushNotification;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TLSharp.Core.MTProto.Crypto;
using Xamarin.Forms;

namespace PagerBuddy.Services
{
    public class MessagingService
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public void SetupListeners(CommunicationService client)
        {
            //subscribe to token changes
            CrossFirebasePushNotification.Current.OnTokenRefresh += async (s, args) =>
                {
                    Logger.Info("Firebase token was updated, TOKEN: {0}", args.Token);

                    if (client.clientStatus == CommunicationService.STATUS.AUTHORISED) {
                        await client.subscribePushNotifications(args.Token);
                    }
                };

            //foreground notification listener
            CrossFirebasePushNotification.Current.OnNotificationReceived += (s, p) =>
                {
                    Logger.Debug("A firebase message was received while app is active.");
                    //new AlertService(client);
                    decodePayload(p);
                };
        }


        //This is called when FCM Messages are received after app is killed
        public static void BackgroundFirebaseMessage(object sender, FirebasePushNotificationDataEventArgs args)
        {
            Logger.Debug("A firebase message was received while app is in background.");
            //new AlertService();
            decodePayload(args);
        }

        public static void BackgroundFirebaseTokenRefresh(object sender, string token) {
            Logger.Debug("Firebase token was updated while app is in background.");

            CommunicationService client = new CommunicationService(true);
            client.StatusChanged += async (sender, status) => {
                if(status == CommunicationService.STATUS.AUTHORISED) {
                    await client.subscribePushNotifications(token);
                }
            };
            
        }

        private static void decodePayload(FirebasePushNotificationDataEventArgs args) {
            IDictionary<string, object> data = args.Data;

            object rawPayload;
            bool success = data.TryGetValue("p", out rawPayload);

            if(!success || !(rawPayload is string)) {
                //No FCM payload
                //TODO: Log this. Is this possibly even an error?
                return;
            }

            //Payload is bas64 URL encoded - we have to get standard base64
            string stringPayload = (rawPayload as string).Replace("-", "+").Replace("_", "/");
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
            if (authKey.Length < 1) {
                //AuthKey not set - cannot decode
                //TODO: Log this
                return;
            }


            byte[] authKeyHash = new SHA1Managed().ComputeHash(authKey);
            byte[] authKeyID = authKeyHash[(authKeyHash.Length-8)..]; //last 8 bytes of hash are ID

            Span<byte> payloadAuthKeyID = new Span<byte>(new byte[8]);
            stream.Read(payloadAuthKeyID);

            if(!Enumerable.SequenceEqual(authKeyID, payloadAuthKeyID.ToArray())) {
                //ID does not match
                //TODO: Log this
                return;
            }

            Span<byte> messageKey = new Span<byte>(new byte[16]);
            stream.Read(messageKey);

            (byte[] aesKey, byte[] aesIv) = getKeyData(authKey, messageKey.ToArray());

            Memory<byte> decrypted = AesIGEEncrypt.Decrypt(bytes[24..], aesKey, aesIv); //start decryption at byte 24
            MemoryStream decryptedStream = new MemoryStream(decrypted.ToArray());
            

            int length;
            using (BinaryReader reader = new BinaryReader(decryptedStream)) {
                length = reader.ReadInt32();
            }

            string stringRaw = Encoding.UTF8.GetString(decrypted.ToArray()[4..]);

            //byte[] strBytes = new byte[length];
            //decryptedStream.Read(new Span<byte>(strBytes));

            //JObject json = JObject.Parse(Encoding.UTF8.GetString(strBytes));
            return;
        }

        private static (byte[] aesKey, byte[] aesIv) getKeyData(byte[] authKey, byte[] messageKey) {

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

    }

    public class AesIGEEncrypt {

        public static Memory<byte> Decrypt(Span<byte> bytes, byte[] key, byte[] iv) {
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
            return decryptedData.AsMemory();
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
