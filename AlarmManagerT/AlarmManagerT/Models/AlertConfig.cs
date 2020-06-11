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

        

    }
}