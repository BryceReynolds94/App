using Newtonsoft.Json.Linq;
using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using Plugin.FirebasePushNotification;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            CrossFirebasePushNotification.Current.OnTokenRefresh += async (object sender, FirebasePushNotificationTokenEventArgs args) => {
                await FirebaseTokenRefresh(args.Token);
            };

            CrossFirebasePushNotification.Current.OnNotificationReceived += (object sender, FirebasePushNotificationDataEventArgs args) => {
                FirebaseMessage(null, DateTime.Now); //TODO: RBF
            };

        }




        //TODO: Clean this up with FCM plugin
        //This is called when FCM Messages are received
        public static void FirebaseMessage(IDictionary<string,string> data, DateTime timestamp)
        {
            inspectPayload(data, timestamp);
        }

        public static async Task FirebaseTokenRefresh(string token) {
            Logger.Info("Firebase token was updated, TOKEN: {0}", token);
            DataService.setConfigValue(DataService.DATA_KEYS.FCM_TOKEN, token);

            Collection<string> configIDs = DataService.getConfigList();
            Collection<AlertConfig> configList = new Collection<AlertConfig>();
            foreach(string configID in configIDs) {
                AlertConfig config = DataService.getAlertConfig(configID, null);
                if(config != null) {
                    configList.Add(config);
                }
            }

            if (instance != null) {
                if (instance.client.clientStatus == CommunicationService.STATUS.AUTHORISED) {
                    await instance.client.sendServerRequest(configList);
                }
            } else {
                CommunicationService client = new CommunicationService(true);
                client.StatusChanged += async (sender, status) => {
                    if (status == CommunicationService.STATUS.AUTHORISED) {
                        await client.sendServerRequest(configList);
                    }
                };
            }  
        }

        private static void inspectPayload(IDictionary<string,string> data, DateTime timestamp) {
           //TODO: Handle message
        }
    }
}
