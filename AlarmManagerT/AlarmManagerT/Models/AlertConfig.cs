using AlarmManagerT.Services;
using AlarmManagerT.ViewModels;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AlarmManagerT.Models
{
    public class AlertConfig 
    {
        public enum TRIGGER_TYPE { ANY, SERVER, KEYWORD, REGEX};

        public bool isActive = true;
        public bool snoozeActive => snoozeTime > DateTime.Now;
        public DateTime snoozeTime = DateTime.MinValue;
        public string id = Guid.NewGuid().ToString();

        public Group triggerGroup;
        public TRIGGER_TYPE triggerType = TRIGGER_TYPE.ANY;
        public string triggerKeyword;

        public ActiveTimeConfig activeTimeConfig = new ActiveTimeConfig();
        public bool timeRestriction = false;

        public bool actionSound = true;
        public string actionRingtone;
        public bool actionVibrate = true;
        public bool actionLight = true;

        public DateTime lastTriggered = DateTime.MinValue;

        public int lastMessageID = 0;

        public void saveChanges()
        {
            Data.saveAlertConfig(this);
        }

        public void setActiveState(bool active)
        {
            isActive = active;
            if (!active)
            {
                snoozeTime = DateTime.MinValue; //clear Snooze Time
            }
            saveChanges();
        }

        public void setSnoozeTime(DateTime snoozeTime)
        {
            this.snoozeTime = snoozeTime;
            saveChanges();
        }

        public Boolean isAlert(string message, DateTime time)
        {
            if (!isActive || snoozeActive) 
            {
                return false;
            }
            if(timeRestriction && !activeTimeConfig.isActiveTime(time))
            {
                return false;
            }

            switch (triggerType)
            {
                case TRIGGER_TYPE.ANY:
                    return true;
                case TRIGGER_TYPE.KEYWORD:
                case TRIGGER_TYPE.REGEX:
                    //TODO: Implement difference or get rid of simple KEYWORD option
                    if (triggerKeyword == null)
                    {
                        return false;
                    }
                    else
                    {
                        return Regex.IsMatch(message, triggerKeyword);
                    }
                case TRIGGER_TYPE.SERVER:
                    //TODO: Invent this
                    break;
            }
            return false;
            
        }
        

    }
}