using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PagerBuddy.Services {
    public class AlertService {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void checkMessage(string message, int senderID, DateTime timestamp, int fromID) {
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

                    if (config.isAlert(message, timestamp, fromID)) {
                        DateTime referenceTime = DateTime.Now.Subtract(new TimeSpan(0, 10, 0)); //grace period of 10min
                        if (timestamp < referenceTime) //timestamp is older than referenceTime
                        {
                            //discard missed messages older than 10min
                            Logger.Info("An alert was dismissed as it was not detected within 10min of message posting. Message posted at (UTC): " + timestamp.ToShortTimeString());
                        } else {
                            config.setLastTriggered(DateTime.Now);
                            alertMessage(new Alert(message, config));
                        }
                    } else {
                        Logger.Debug("Message ignored, it did not fulfill the alert criteria.");
                    }
                }
            }
        }

        private static void alertMessage(Alert alert) {
            Logger.Info("Alert was detected. Posting it to notifications.");

            INotifications notifications = DependencyService.Get<INotifications>();
            notifications.showAlertNotification(alert);
        }

    }
}
