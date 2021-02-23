using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using Plugin.FirebasePushNotification;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using TeleSharp.TL;
using TeleSharp.TL.Messages;
using Xamarin.Forms;

namespace PagerBuddy.Services {
    public class AlertService {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public AlertService(string message, int senderID, long timestampTicks) {
            checkMessage(message, senderID, timestampTicks);
        }


        private void checkMessage(string message, int senderID, long timestampTicks) {
            Logger.Info("Checking incoming message for alert.");

            Collection<AlertConfig> configList = new Collection<AlertConfig>();
            Collection<string> configIDs = DataService.getConfigList();
            foreach (string id in configIDs) {
                configList.Add(DataService.getAlertConfig(id));
            }

            if (configList.Count < 1) {
                Logger.Debug("Configuration list is empty.");
                return;
            }

            foreach (AlertConfig config in configList) {
                if(config.triggerGroup.id == senderID) {
                    Logger.Debug("New message for alert config " + config.readableFullName);

                    DateTime timestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestampTicks).ToLocalTime();  //Unix base time -- we need local time for alert time comparison
                    if (config.isAlert(message, timestamp, msg.Entities)) {
                        DateTime referenceTime = DateTime.Now.Subtract(new TimeSpan(0, 10, 0)); //grace period of 10min
                        if (timestamp < referenceTime) //timestamp is older than referenceTime
                        {
                            //discard missed messages older than 10min
                            Logger.Info("An alert was dismissed as it was not detected within 10min of message posting. Message posted at (UTC): " + timestamp.ToShortTimeString());
                        } else {
                            config.setLastTriggered(DateTime.Now);
                            alertMessage(new Alert(config.getAlertMessage(message, msg.Entities), config));
                        }
                    } else {
                        Logger.Debug("Message ignored, it did not fulfill the alert criteria.");
                    }
                }
            }
        }

        private void alertMessage(Alert alert) {
            Logger.Info("Alert was detected. Posting it to notifications.");

            INotifications notifications = DependencyService.Get<INotifications>();
            notifications.showAlertNotification(alert);
        }

    }
}
