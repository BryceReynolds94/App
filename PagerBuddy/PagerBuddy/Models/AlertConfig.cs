using PagerBuddy.Interfaces;
using PagerBuddy.Services;
using PagerBuddy.ViewModels;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace PagerBuddy.Models
{
    public class AlertConfig 
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public enum TRIGGER_TYPE { ANY, SERVER, KEYWORD};

        public bool isActive = true;
        public bool snoozeActive => snoozeTime > DateTime.Now;
        public DateTime snoozeTime = DateTime.MinValue;
        public string id = Guid.NewGuid().ToString();

        public Group triggerGroup;
        public TRIGGER_TYPE triggerType = TRIGGER_TYPE.ANY;
        public string triggerKeyword;

        public ActiveTimeConfig activeTimeConfig = new ActiveTimeConfig();
        public bool timeRestriction = false;

        public DateTime lastTriggered = DateTime.MinValue;
        public DateTime lockTime = DateTime.MinValue;

        public void saveChanges()
        {
            DataService.saveAlertConfig(this);
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
        
        public bool isAlert(string message, DateTime time)
        {
            if (!isActive || snoozeActive) 
            {
                return false;
            }
            if(timeRestriction && !activeTimeConfig.isActiveTime(time))
            {
                return false;
            }

            bool result = false;
            switch (triggerType)
            {
                case TRIGGER_TYPE.ANY:
                    result = true;
                    break;
                case TRIGGER_TYPE.KEYWORD:
                    result = (!(triggerKeyword == null)) && Regex.IsMatch(message, triggerKeyword);
                    break;
                case TRIGGER_TYPE.SERVER:
                    //TODO Later: Invent PagerBuddy-Server syntax
                    break;
            }

            if (result) {
                if(lockTime > time) {
                    result = false;
                    Logger.Info("Suppressed alert as insufficient time has passed since the last qualified alert message.");
                }
                lockTime = time.AddMinutes(5); //Require no message that would qualify as alert for 5 minutes before next alert
            }

            return result;   
        }
        

    }
}