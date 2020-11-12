using System;
using System.Collections.Generic;
using System.Text;

namespace AlarmManagerT.Models
{
    public class Alert
    {
        public enum EXTRAS { ALERT_FLAG };

        public string title;
        public string text;
        public string configID;
        public int chatID;
        public bool hasPic;

        public Alert(string message, AlertConfig config)
        {
            title = config.triggerGroup.name;
            text = message;
            configID = config.id;
            chatID = config.triggerGroup.id;
            hasPic = config.triggerGroup.hasImage;
        }

        public Alert(string title, string text, string configID, int chatID, bool hasPic)
        {
            this.title = title;
            this.text = text;
            this.configID = configID;
            this.chatID = chatID;
            this.hasPic = hasPic;
        }

        public static Alert getTestSample(string testPoint) //TODO: RBF
        {
            return new Alert("TEST ALERT", testPoint, "NO GUID AS TEST", 1234, false);
        }

    }
}
