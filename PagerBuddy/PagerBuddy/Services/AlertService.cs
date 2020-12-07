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

        public AlertService(CommunicationService client) {
            checkNewMessages(client);
        }

        public AlertService() {
            if (checkLockTime(DateTime.Now)) {
                return;
            }

            CommunicationService client = new CommunicationService(true); //only perform small client init and do not retry on fatal errors
            client.StatusChanged += (sender, newStatus) => {
                if (newStatus == CommunicationService.STATUS.AUTHORISED) {
                    checkNewMessages(client);
                }
            };
        }

        private bool checkLockTime(DateTime currentTime) {
            DateTime lockTime = DataService.getConfigValue(DataService.DATA_KEYS.REFRESH_LOCK_TIME, DateTime.MinValue);
            DateTime lastRefreshTime = DataService.getConfigValue(DataService.DATA_KEYS.LAST_REFRESH_TIME, DateTime.MinValue);

            int lockDuration = 2000; //milliseconds

            if (lockTime > currentTime) {
                return true; //we are in time lock - do nothing
            } else if (lastRefreshTime.AddMilliseconds(lockDuration) > currentTime) {
                //short succession updates
                //set 2s lock time and snooze 2s
                DataService.setConfigValue(DataService.DATA_KEYS.REFRESH_LOCK_TIME, currentTime.AddMilliseconds(lockDuration));
                DataService.setConfigValue(DataService.DATA_KEYS.LAST_REFRESH_TIME, currentTime.AddMilliseconds(lockDuration));

                Logger.Debug("Quick succession updates. Setting lock time for 2s.");

                Task.Delay(lockDuration).Wait();
                Logger.Debug("Lock time over.");
                return false;
            }
            //continue as planned
            DataService.setConfigValue(DataService.DATA_KEYS.LAST_REFRESH_TIME, currentTime);
            return false;
        }


        private async void checkNewMessages(CommunicationService client) {
            Logger.Info("Checking for new messages.");

            Collection<AlertConfig> configList = new Collection<AlertConfig>();
            Collection<string> configIDs = DataService.getConfigList();
            foreach (string id in configIDs) {
                configList.Add(DataService.getAlertConfig(id));
            }

            if(configList.Count < 1) {
                Logger.Debug("Configuration list is empty. Will not check messages, but update message ID pointer.");
            }

            int currentMessageID = DataService.getConfigValue(DataService.DATA_KEYS.LAST_MESSAGE_ID, 0);

            foreach (AlertConfig config in configList) {
                TLAbsMessages result = await client.getMessages(config.triggerGroup.id, currentMessageID);
                TLVector<TLAbsMessage> messageList;

                if(result == null) {
                    Logger.Error("Retrieving messages returned null.");
                    break;
                }

                    if (result is TLMessages) {
                        messageList = (result as TLMessages).Messages;
                    } else if (result is TLMessagesSlice) {
                        messageList = (result as TLMessagesSlice).Messages;
                    } else {
                        Logger.Warn("Retrieving Messages from Telegram did not yield a valid message type.");
                        break; //we did not get valid result 
                    }


                if (messageList.Count < 1) {
                    Logger.Debug("No new messages for AlertConfig " + config.triggerGroup.name);
                    break;
                }

                foreach (TLAbsMessage rawMessage in messageList) {
                    if (!(rawMessage is TLMessage)) {
                        break;
                    }
                    TLMessage msg = rawMessage as TLMessage;

                    DateTime timestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(msg.Date).ToLocalTime();  //Unix base time -- we need locacl time for alert time comparison

                    if (config.isAlert(msg.Message, timestamp)) {
                        DateTime referenceTime = DateTime.Now.Subtract(new TimeSpan(0, 10, 0)); //grace period of 10min
                        if (timestamp < referenceTime) //timestamp is older than referenceTime
                        {
                            //discard missed messages older than 10min
                            Logger.Info("An alert was dismissed as it was not detected within 10min of message posting. Message posted at (UTC): " + timestamp.ToShortTimeString());
                        } else {
                            config.lastTriggered = DateTime.Now;
                            alertMessage(config, msg);
                        }
                    }
                }

                //persists values to clean up
                config.saveChanges();
            }
            //update message id index
            DataService.setConfigValue(DataService.DATA_KEYS.LAST_MESSAGE_ID, await client.getLastMessageID(currentMessageID));
        }

        private void alertMessage(AlertConfig config, TLMessage message) {
            Logger.Info("Alert was detected. Posting it to notifications.");

            INotifications notifications = DependencyService.Get<INotifications>();
            notifications.showAlertNotification(new Alert(message.Message, config));
        }

    }
}
