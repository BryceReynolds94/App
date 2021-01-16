using PagerBuddy.Interfaces;
using PagerBuddy.Services;
using PagerBuddy.ViewModels;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Xamarin.Forms;
using System.Text;
using System.Security.Cryptography;

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
                    result = hasPagerBuddyPayload(message);
                    Logger.Debug("Trigger type is SERVER. Result of Payload check: " + result);
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

        [JsonIgnore]
        public string readableFullName {
            get {
                string output = triggerGroup.name;
                if (triggerType == TRIGGER_TYPE.KEYWORD && triggerKeyword != null && triggerKeyword.Length > 0) {
                    output = output + " - " + triggerKeyword;
                }
                return output;
            }
        }

        private bool hasPagerBuddyPayload(string message) {
            //PagerBuddy-Server syntax:
            //Encode alert in <html/> tag as part of URL
            //Alerts should contain url "*/pagerbuddy?#<base64><title/>*<date and time/>*<optional message/></base64>#"
            Match match = extractPagerBuddyPayload(message);
            return match.Success;
        }

        private Match extractPagerBuddyPayload(string rawMessage) {
            return Regex.Match(rawMessage, "/pagerbuddy\\?#[-A-Za-z0-9+/]*={0,3}#"); //Regex: Match "/pagerbuddy?# <valid base 64 characters (A-Z, a-z, 0-9, +, /; followed by 0-3 "=")> #"
        }

        public string getAlertMessage(string rawMessage) {
            if(triggerType != TRIGGER_TYPE.SERVER) {
                //Server trigger not used - simply return rawMessage
                return rawMessage;
            } else {
                Match match = extractPagerBuddyPayload(rawMessage);
                if (!match.Success) {
                    Logger.Warn("Could not match server RegEx in apparent PagerBuddy-Server message. Returning rawMessage.");
                    return rawMessage;
                }
                try {
                    string encodedMessage = match.Value.Split("#")[1]; //We want the stuff between the two #
                    string decodedMessage = Encoding.UTF8.GetString(Convert.FromBase64String(encodedMessage)); //decode base64, message info in the format title*date time*optional message

                    return decodedMessage.Replace("*", "\r\n"); //put line breaks between the info sections
                }catch(Exception e) {
                    Logger.Error(e, "An exception occured trying to parse the PagerBuddy-Server string. Returning rawMessage instead.");
                    return rawMessage;
                }
            }
        }
        

    }
}