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

            CommunicationService client = new CommunicationService();
            client.StatusChanged += (sender, args) => {
                if (client.clientStatus == CommunicationService.STATUS.AUTHORISED) {
                    checkNewMessages(client);
                }
            };
        }

        private bool checkLockTime(DateTime currentTime) {
            DateTime lockTime = new DateTime(DataService.getConfigValue<long>(DataService.DATA_KEYS.REFRESH_LOCK_TIME, DateTime.MinValue.Ticks));
            DateTime lastRefreshTime = new DateTime(DataService.getConfigValue<long>(DataService.DATA_KEYS.LAST_REFRESH_TIME, DateTime.MinValue.Ticks));

            int lockDuration = 2000; //milliseconds

            if (lockTime.CompareTo(currentTime) > 0) {
                return true; //we are in time lock - do nothing
            } else if (lastRefreshTime.AddMilliseconds(lockDuration).CompareTo(currentTime) > 0) {
                //short succession updates
                //set 2s lock time and snooze 2s
                DataService.setConfigValue(DataService.DATA_KEYS.REFRESH_LOCK_TIME, currentTime.AddMilliseconds(lockDuration).Ticks);
                DataService.setConfigValue(DataService.DATA_KEYS.LAST_REFRESH_TIME, currentTime.AddMilliseconds(lockDuration).Ticks);

                Logger.Debug("Quick succession updates. Setting lock time for 2s.");

                Task.Delay(lockDuration).Wait();
                Logger.Debug("Lock time over.");
                return false;
            }
            //continue as planned
            DataService.setConfigValue(DataService.DATA_KEYS.LAST_REFRESH_TIME, currentTime.Ticks);
            return false;
        }


        private async void checkNewMessages(CommunicationService client) {
            Collection<AlertConfig> configList = new Collection<AlertConfig>();
            Collection<string> configIDs = DataService.getConfigList();
            foreach (string id in configIDs) {
                configList.Add(DataService.getAlertConfig(id));
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
                    break;
                }

                foreach (TLAbsMessage rawMessage in messageList) {
                    if (!(rawMessage is TLMessage)) {
                        break;
                    }
                    TLMessage msg = rawMessage as TLMessage;

                    DateTime timestamp = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(msg.Date);  //Unix base time

                    if (config.isAlert(msg.Message, timestamp)) {
                        DateTime referenceTime = DateTime.Now.Subtract(new TimeSpan(0, 10, 0)); //grace period of 10min
                        if (timestamp.CompareTo(referenceTime) < 0) //timestamp is older than referenceTime
                        {
                            //discard missed messages older than 10min
                            Logger.Info("An alert was dismissed as it was not detected within 10min of message posting");
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
            INotifications notifications = DependencyService.Get<INotifications>();
            notifications.showAlertNotification(new Alert(message.Message, config));

            Logger.Info("Alert was detected. Posting it to notifications.");
        }

    }
}
