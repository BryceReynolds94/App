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

        private readonly CommunicationService client;
        private static MessagingService instance;

        public MessagingService(CommunicationService client) {
            this.client = client;
            instance = this;
        }


        //This is called when FCM Messages are received
        public static void FirebaseMessage(IDictionary<string,string> data, DateTime timestamp)
        {
            inspectPayload(data, timestamp);
        }

        public static async Task FirebaseTokenRefresh(string token) {
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

        private static void inspectPayload(IDictionary<string,string> data, DateTime timestamp) {
           //TODO: Handle message
        }
    }
}
