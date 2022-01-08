using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace PagerBuddy.Models
{
    public class Alert
    {
        public enum EXTRAS { ALERT_FLAG };

        public string title;
        public string text;
        public string configID;
        public long chatID;
        public bool hasPic;
        public DateTime timestamp;
        public bool isTestAlert;

        public TelegramPeer.TYPE peerType;

        public Alert(string message, DateTime timestamp, bool isTestAlert, AlertConfig config)
        {
            title = config.triggerGroup.name;
            text = message;
            this.timestamp = timestamp;
            configID = config.id;
            chatID = config.triggerGroup.id;
            hasPic = config.triggerGroup.hasImage;
            this.isTestAlert = isTestAlert;

            peerType = config.triggerGroup.type;
        }

        [JsonConstructor]
        public Alert(string title, string text, string configID, long chatID, bool hasPic, DateTime timestamp, bool isTestAlert, TelegramPeer.TYPE peerType)
        {
            this.title = title;
            this.text = text;
            this.configID = configID;
            this.chatID = chatID;
            this.hasPic = hasPic;
            this.timestamp = timestamp;
            this.peerType = peerType;
            this.isTestAlert = isTestAlert;
        }

        public string description {
            get {
                string alertText = "";
                if (isTestAlert) {
                    alertText += Resources.AppResources.Alert_TestFilter + Environment.NewLine;
                }
                alertText += text + Environment.NewLine;
                alertText += timestamp.ToString();

                return alertText;
            }
        }

    }
}
