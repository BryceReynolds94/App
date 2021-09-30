using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Xamarin.Forms;

namespace PagerBuddy.Models {
    public class ServerRequest {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public enum MESSAGING_TYPE { FCM, APNS}

        public MESSAGING_TYPE MessagingType;
        public string Token;
        public Collection<int> AlertIDList;

        public ServerRequest(MESSAGING_TYPE messagingType, string token, Collection<int> alertIDList) {
            this.MessagingType = messagingType;
            this.Token = token;
            this.AlertIDList = alertIDList;
        }

        public static ServerRequest getServerRequest(Collection<AlertConfig> configList) {

            Collection<int> alertIDList = new Collection<int>();
            foreach(AlertConfig config in configList) {
                if (config.isActive) {
                    alertIDList.Add(config.triggerGroup.id);
                }
            }

            string token;
            if (Device.RuntimePlatform == Device.Android) {
                token = DataService.getConfigValue(DataService.DATA_KEYS.FCM_TOKEN, "");
                if (token == null || token.Length < 1) {
                    Logger.Warn("Token invalid. Cannot construct ServerRequest");
                    return null;
                }
                return new ServerRequest(MESSAGING_TYPE.FCM, token, alertIDList);

            } else if (Device.RuntimePlatform == Device.iOS) {
                token = ""; //TODO: IOS Implement APNS token

                return new ServerRequest(MESSAGING_TYPE.APNS, token, alertIDList);

            } else {
                Logger.Error("Device platform was of unexpected type. This should never happen. The current RuntimePLatform is " + Device.RuntimePlatform);
                return null;
            }
        }
    }
}
