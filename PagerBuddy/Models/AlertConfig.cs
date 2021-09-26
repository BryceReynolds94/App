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
using Types = Telega.Rpc.Dto.Types;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace PagerBuddy.Models {
    public class AlertConfig {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private const int PAGERBUDDY_ID = 1600415343;

        public bool isActive { get; private set; }

        public string id { get; private set; }

        public Group triggerGroup;

        public DateTime lastTriggered { get; private set; }
        [JsonProperty]
        private DateTime lockTime;

        public AlertConfig(Group triggerGroup) {
            id = Guid.NewGuid().ToString();
            this.triggerGroup = triggerGroup;


            isActive = true;
            lastTriggered = lockTime = DateTime.MinValue;
        }

        [JsonConstructor]
        public AlertConfig(bool isActive, string id, Group triggerGroup, DateTime lastTriggered, DateTime lockTime) {
            this.isActive = isActive;
            this.id = id;
            this.triggerGroup = triggerGroup;
            this.lastTriggered = lastTriggered;
            this.lockTime = lockTime;
        }

        public static AlertConfig findExistingConfig(int triggerGroupID) {
            Collection<string> configList = DataService.getConfigList();
            foreach(string config in configList) {
                AlertConfig alertConfig = DataService.getAlertConfig(config);
                if (alertConfig != null && alertConfig.triggerGroup.id == triggerGroupID) {
                    return alertConfig;
                }
            }
            return null;
        }

        public void saveChanges(bool persistImage = false) {
            if (persistImage && triggerGroup.hasImage && triggerGroup.image != null) {
                DataService.saveProfilePic(id, triggerGroup.image);
            }
            DataService.saveAlertConfig(this);
        }

        public void setActiveState(bool active) {
            isActive = active;
            saveChanges();
        }


        public void setLastTriggered(DateTime triggerTime) {
            lastTriggered = triggerTime;
            saveChanges();
        }

        public bool isAlert(string message, DateTime time, int fromID) {
            if (!isActive) {
                Logger.Debug("Alert not triggered as AlertConfig is inactive. AlertConfig: " + readableFullName);
                return false;
            }

            bool result = isPagerBuddyMessage(fromID);
            /*if (result) {
                result = !isPagerBuddyTestAlert(message);
            }*/ //TODO: Implement test alert changes
            Logger.Debug("Result of sender check: " + result);

            if (result) {
                if (lockTime > time) {
                    result = false;
                    Logger.Info("Suppressed alert as insufficient time has passed since the last qualified alert message.");
                }
                lockTime = time.AddMinutes(5); //Require no message that would qualify as alert for 5 minutes before next alert
                saveChanges();
            }

            return result;
        }

        [JsonIgnore]
        public string readableFullName => triggerGroup.name;

        private static bool isPagerBuddyMessage(int fromID) {
            return fromID == PAGERBUDDY_ID;
        }

        private static bool isPagerBuddyTestAlert(string message) {
            //Last digit of payload contains 1 or 0 signifying if this is a test alert
            Match match = Regex.Match(message, "(?<=#)[-A-Za-z0-9+/]*={0,3}(?=#)"); //Regex: Match "# <valid base 64 characters (A-Z, a-z, 0-9, +, /; followed by 0-3 "=")> #"
            if (!match.Success) {
                return false;
            }

            string[] segments;
            try {
                string decodedMessage = Encoding.UTF8.GetString(Convert.FromBase64String(match.Value)); //decode base64, message info in the format timestamp*is test alert
                segments = decodedMessage.Split("*");
            } catch (Exception e) {
                Logger.Error(e, "An exception occured trying to parse the PagerBuddy-Server string.");
                return false;
            }

            if (segments.Length < 2) {
                return false;
            }

            return segments[1].Equals("1");

        }

    }
}