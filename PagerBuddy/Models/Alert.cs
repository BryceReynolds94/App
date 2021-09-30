﻿using Newtonsoft.Json;
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
        public int chatID;
        public bool hasPic;

        public TelegramPeer.TYPE peerType;

        public Alert(string message, AlertConfig config)
        {
            title = config.triggerGroup.name;
            text = message;
            configID = config.id;
            chatID = config.triggerGroup.id;
            hasPic = config.triggerGroup.hasImage;

            peerType = config.triggerGroup.type;
        }

        [JsonConstructor]
        public Alert(string title, string text, string configID, int chatID, bool hasPic, TelegramPeer.TYPE type)
        {
            this.title = title;
            this.text = text;
            this.configID = configID;
            this.chatID = chatID;
            this.hasPic = hasPic;
            this.peerType = type;
        }

    }
}