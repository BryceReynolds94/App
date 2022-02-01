﻿using Newtonsoft.Json.Linq;
using PagerBuddy.Interfaces;
using PagerBuddy.Models;
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
        }

        //This is called when FCM Messages are received
        public static void FirebaseMessage(IDictionary<string,string> data)
        {
            if (data.Count < 5) {
                Logger.Warn("Received an FCM/APNS message with an invalid payload count. Ignoring message.");
                _ = LegacyMessageReceived();
                return;
            }

            bool res;
            res = data.TryGetValue("alert_timestamp", out string timestampR);
            res &= data.TryGetValue("zvei", out string zvei);
            res &= data.TryGetValue("is_test_alert", out string testAlertR);
            res &= data.TryGetValue("zvei_description", out string description);
            res &= data.TryGetValue("chat_id", out string chatIDR);
            res &= data.TryGetValue("is_manual_test_alert", out string manualTestR);

            res &= long.TryParse(timestampR, out long timestamp);
            res &= bool.TryParse(testAlertR, out bool testAlert);
            res &= bool.TryParse(manualTestR, out bool manualTest);
            res &= long.TryParse(chatIDR, out long chatID);

            //TODO: RBF - We have a parse error on test alert. Test for debugging
            if (!res) {
                Logger.Warn("Error parsing payload. Ignoring message. Payload: " + string.Join(",", data));
                return;
            }

            DateTime alertTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timestamp).ToLocalTime(); //Unix base time -- we need local time for alert time comparison
            AlertService.checkMessage(description, chatID, alertTime, testAlert, manualTest);
        }

        public static void TokenRefresh(string token) {
            if(DataService.getConfigValue(DataService.DATA_KEYS.FCM_TOKEN, "") == token) {
                Logger.Debug("Token refresh received, but token has not changed.");
                return;
            }

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
            scheduler.scheduleRequest(configList, CommunicationService.pagerbuddyServerList.First()); //TODO Later: MULTI-Server
        }

        //TODO: Later - Remove this once all legacy users migrated
        public static async Task LegacyMessageReceived() {

            string token = DataService.getConfigValue(DataService.DATA_KEYS.FCM_TOKEN, "");
            if(instance == null) {
                CommunicationService client = new CommunicationService();
                client.StatusChanged += async (object sender, CommunicationService.STATUS status) => {
                    if(status == CommunicationService.STATUS.AUTHORISED) {
                        await client.legacyUnregister(token);
                    };
                };
                _ = client.connectClient(true);
            } else {
                await instance.client?.legacyUnregister(token);
            }

        }
    }
}
