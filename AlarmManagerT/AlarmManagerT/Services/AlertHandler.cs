using AlarmManagerT.Interfaces;
using AlarmManagerT.Models;
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

namespace AlarmManagerT.Services
{
    public class AlertHandler
    {
        public void checkForNewMessages(MyClient client)
        {
            checkNewMessages(client);
        }

        public void checkForNewMessages()
        {
            if (checkLockTime(DateTime.Now))
            {
                return;
            }

            MyClient client = new MyClient();
            client.StatusChanged += (sender, args) => { 
                if(client.getClientStatus() == MyClient.STATUS.AUTHORISED) 
                { 
                    checkForNewMessages(client); 
                } 
            };
        }

        private Boolean checkLockTime(DateTime currentTime)
        {
            DateTime lockTime = new DateTime(Data.getConfigValue<long>(Data.DATA_KEYS.REFRESH_LOCK_TIME, DateTime.MinValue.Ticks));
            DateTime lastRefreshTime = new DateTime(Data.getConfigValue<long>(Data.DATA_KEYS.LAST_REFRESH_TIME, DateTime.MinValue.Ticks));

            int lockDuration = 2000; //milliseconds

            if (lockTime.CompareTo(currentTime) > 0)
            {
                return true; //we are in time lock - do nothing
            }else if(lastRefreshTime.AddMilliseconds(lockDuration).CompareTo(currentTime) > 0)
            {
                //short succession updates
                //set 2s lock time and snooze 2s
                Data.setConfigValue(Data.DATA_KEYS.REFRESH_LOCK_TIME, currentTime.AddMilliseconds(lockDuration).Ticks);
                Data.setConfigValue(Data.DATA_KEYS.LAST_REFRESH_TIME, currentTime.AddMilliseconds(lockDuration).Ticks);

                Task.Delay(lockDuration).Wait(); //TODO: Unsure about this
                return false;
            }
            //continue as planned
            Data.setConfigValue(Data.DATA_KEYS.LAST_REFRESH_TIME, currentTime.Ticks);
            return false;
        }


        private async void checkNewMessages(MyClient client)
        {
            Collection<AlertConfig> configList = new Collection<AlertConfig>();
            Collection<string> configIDs = Data.getConfigList();
            foreach (string id in configIDs)
            {
                configList.Add(Data.getAlertConfig(id));
            }
            
            foreach (AlertConfig config in configList)
            {
                TLAbsMessages result = await client.getMessages(config.triggerGroup.id, config.lastMessageID);
                TLVector<TLAbsMessage> messageList;

                if(result is TLMessages)
                {
                    messageList = (result as TLMessages).Messages;
                }else if(result is TLMessagesSlice)
                {
                    messageList = (result as TLMessagesSlice).Messages;
                }
                else
                {
                    //TODO: Log
                    break; //we did not get valid result 
                }


                if (messageList.Count < 1)
                {
                    break;
                }
                else
                {
                    foreach(TLAbsMessage rawMessage in messageList)
                    {
                        if (rawMessage is TLMessage)
                        {
                            config.lastMessageID = (rawMessage as TLMessage).Id;
                            break;
                        }
                        else
                        {
                            messageList.Remove(rawMessage);
                        }
                    }
                }

                foreach(TLAbsMessage rawMessage in messageList)
                {
                    //TODO: Test this

                    if(!(rawMessage is TLMessage))
                    {
                        break;
                    }
                    TLMessage msg = rawMessage as TLMessage;

                    DateTime timestamp = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(msg.Date);  //Unix base time
                    
                    if (config.isAlert(msg.Message, timestamp))
                    {
                        DateTime referenceTime = DateTime.Now.Subtract(new TimeSpan(0, 10, 0)); //grace period of 10min
                        if (timestamp.CompareTo(referenceTime) < 0) //timestamp is older than referenceTime
                        {
                            //discard missed messages older than 10min
                            //TODO: Log this - possibly notify user
                        }
                        else
                        {
                            config.lastTriggered = DateTime.Now;
                            alertMessage(config, msg);
                        }
                    }
                }

                //persists values to clean up
                config.saveChanges();
            }
        }

        private void alertMessage(AlertConfig config, TLMessage message)
        {
            INotifications notifications = DependencyService.Get<INotifications>();
            notifications.showAlertNotification(new Alert(message.Message, config));

            return; //TODO: RBF
        }

    }
}
