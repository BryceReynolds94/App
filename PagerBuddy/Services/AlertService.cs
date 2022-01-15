using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using PagerBuddy.Resources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PagerBuddy.Services {
    public class AlertService {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void checkMessage(string message, long chatID, DateTime timestamp, bool isTestAlert, bool isManualTest) {
            if(DataService.getConfigValue(DataService.DATA_KEYS.CONFIG_DEACTIVATE_ALL, false)) {
                Logger.Info("All alerts are deactivated. Ignoring incoming message.");
                return;
            }
            if(DataService.getConfigValue(DataService.DATA_KEYS.CONFIG_SNOOZE_ALL, DateTime.MinValue) > DateTime.Now) {
                Logger.Info("All alerts are snoozed. Ignoring incoming message.");
                return;
            }

            //Check if we are in active time limit
            Collection<DayOfWeek> activeDays = DataService.activeDays;
            bool inactiveDay =!activeDays.Contains(DateTime.Today.DayOfWeek);

            TimeSpan fromTime = new TimeSpan(DataService.getConfigValue(DataService.DATA_KEYS.ACTIVE_TIME_FROM, 0));
            TimeSpan toTime = new TimeSpan(DataService.getConfigValue(DataService.DATA_KEYS.ACTIVE_TIME_TO, 0));
            TimeSpan now = DateTime.Now.TimeOfDay;
            bool inactiveTime;
            //If toTime<fromTime assume user ment the next day
            if (toTime < fromTime) {
                inactiveTime = now > fromTime || now < toTime;
            } else {
                inactiveTime = now < toTime && now > fromTime;
            }

            bool invertTime = DataService.getConfigValue(DataService.DATA_KEYS.ACTIVE_TIME_INVERT, false);
            if ((!invertTime && (inactiveTime || inactiveDay)) || (invertTime && (!inactiveTime || !inactiveDay))) {
                Logger.Info("All alerts snoozed due to inactive time. Ignoring incoming message.");
                return;
            }


            Logger.Info("Checking incoming message for alert.");

            Collection<string> configIDs = DataService.getConfigList();
            if (configIDs.Count < 1) {
                Logger.Debug("Configuration list is empty.");
                return;
            }

            foreach (string id in configIDs) {
                AlertConfig config = DataService.getAlertConfig(id, null);
                if (config != null && (config.triggerGroup.serverID == chatID || isManualTest)) {
                    Logger.Debug("New message for alert config " + config.readableFullName);
                    if (isManualTest) {
                        Logger.Debug("Message is a manual test alert.");
                        alertMessage(new Alert(message, timestamp, isTestAlert, config));
                        return;
                    }
                    if (config.isAlert(timestamp)) {
                        DateTime referenceTime = DateTime.Now.Subtract(new TimeSpan(0, 10, 0)); //grace period of 10min
                        if (timestamp < referenceTime) //timestamp is older than referenceTime
                        {
                            //discard missed messages older than 10min
                            Logger.Warn("An alert was dismissed as it was not detected within 10min of message posting. Message posted at (UTC): " + timestamp.ToShortTimeString());
                        } else {
                            if (!isTestAlert) {
                                config.setLastTriggered(timestamp);
                            }
                            alertMessage(new Alert(message, timestamp, isTestAlert, config));
                        }
                    }
                    break;
                }
            }
        }

        private static void alertMessage(Alert alert) {

            if (Device.RuntimePlatform == Device.Android) {
                Logger.Info("Alert was detected. Posting it to notifications.");

                IAndroidNotification notifications = DependencyService.Get<IAndroidNotification>();
                if (alert.isTestAlert) {
                    notifications.showStandardNotification(alert.title, alert.description);
                } else {
                    notifications.showAlertNotification(alert);
                }
            }
        }

    }
}
