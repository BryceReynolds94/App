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
    public class AlertConfig {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public enum TRIGGER_TYPE { ANY, SERVER, KEYWORD };

        public bool isActive { get; private set; }
        public bool snoozeActive => snoozeTime > DateTime.Now;
        public DateTime snoozeTime { get; private set; }
        public string id { get; private set; }

        public Group triggerGroup;
        public TRIGGER_TYPE triggerType;
        public string triggerKeyword;

        public ActiveTimeConfig activeTimeConfig;
        public bool timeRestriction;

        public DateTime lastTriggered { get; private set; }
        [JsonProperty]
        private DateTime lockTime;

        public AlertConfig() {
            isActive = true;
            snoozeTime = DateTime.MinValue;
            id = Guid.NewGuid().ToString();
            triggerType = TRIGGER_TYPE.ANY;
            triggerKeyword = string.Empty;
            activeTimeConfig = new ActiveTimeConfig();
            timeRestriction = false;
            lastTriggered = lockTime = DateTime.MinValue;
        }

        [JsonConstructor]
        public AlertConfig(bool isActive, DateTime snoozeTime, string id, Group triggerGroup, TRIGGER_TYPE triggerType, ActiveTimeConfig activeTimeConfig, bool timeRestriction, DateTime lastTriggered, DateTime lockTime) {
            this.isActive = isActive;
            this.snoozeTime = snoozeTime;
            this.id = id;
            this.triggerGroup = triggerGroup;
            this.triggerType = triggerType;
            this.activeTimeConfig = activeTimeConfig;
            this.timeRestriction = timeRestriction;
            this.lastTriggered = lastTriggered;
            this.lockTime = lockTime;
        }

        public void saveChanges(bool persistImage = false)
        {
            if (persistImage && triggerGroup.hasImage && triggerGroup.image != null) {
                DataService.saveProfilePic(id, triggerGroup.image);
            }
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

        public void setLastTriggered(DateTime triggerTime) {
            lastTriggered = triggerTime;
            saveChanges();
        }
        
        public bool isAlert(string message, DateTime time)
        {
            if (!isActive || snoozeActive) 
            {
                Logger.Debug("Alert not triggered as AlertConfig is inactive. AlertConfig: " + readableFullName);
                return false;
            }
            if(timeRestriction && !activeTimeConfig.isActiveTime(time))
            {
                Logger.Debug("Alert not triggered as time restriction was not fulfilled. AlertConfig: " + readableFullName);
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
                    Logger.Debug("Trigger type is KEYWORD. Result of keyword check: " + result);
                    break;
                case TRIGGER_TYPE.SERVER:
                    //TODO Later: Invent PagerBuddy-Server syntax
                    Logger.Warn("Alert cannot be triggered, as the trigger type is set to SERVER. This method is not implemented yet.");
                    break;
            }

            if (result) {
                if(lockTime > time) {
                    result = false;
                    Logger.Info("Suppressed alert as insufficient time has passed since the last qualified alert message.");
                }
                lockTime = time.AddMinutes(5); //Require no message that would qualify as alert for 5 minutes before next alert
                saveChanges();
            }

            return result;   
        }

        public string readableFullName {
            get {
                string output = triggerGroup.name;
                if (triggerType == TRIGGER_TYPE.KEYWORD && triggerKeyword != null && triggerKeyword.Length > 0) {
                    output = output + " - " + triggerKeyword;
                }
                return output;
            }
        }
        

    }
}