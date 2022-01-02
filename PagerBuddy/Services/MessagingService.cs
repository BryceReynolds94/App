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

            CrossFirebasePushNotification.Current.OnTokenRefresh += (object sender, FirebasePushNotificationTokenEventArgs args) => {
                TokenRefresh(args.Token);
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

        public static void TokenRefresh(string token) {
            Logger.Info("FCM/APNS token was updated, TOKEN: {0}", token);
            DataService.setConfigValue(DataService.DATA_KEYS.FCM_TOKEN, token);

            Collection<string> configIDs = DataService.getConfigList();
            Collection<AlertConfig> configList = new Collection<AlertConfig>();
            foreach(string configID in configIDs) {
                AlertConfig config = DataService.getAlertConfig(configID, null);
                if(config != null) {
                    configList.Add(config);
                }
            }

            if(configList.Count < 1) {
                return;
            }

            IRequestScheduler scheduler = DependencyService.Get<IRequestScheduler>();
            if (instance != null) {
                scheduler.initialise(instance.client);
            }
            scheduler.scheduleRequest(configList, CommunicationService.pagerbuddyServerList.First()); //TODO: Allow all bot types
        }

        private static void inspectPayload(IDictionary<string,string> data, DateTime timestamp) {
           //TODO: Handle message
        }
    }
}
