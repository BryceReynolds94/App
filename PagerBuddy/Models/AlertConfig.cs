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

        public static AlertConfig findExistingConfig(long triggerGroupID) {
            Collection<string> configList = DataService.getConfigList();
            foreach (string config in configList) {
                AlertConfig alertConfig = DataService.getAlertConfig(config, null);
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

        public bool isAlert(DateTime time) {
            if (!isActive) {
                Logger.Debug("Alert not triggered as AlertConfig is inactive. AlertConfig: " + readableFullName);
                return false;
            }

            bool result = true;
            if (lockTime > time) {
                result = false;
                Logger.Info("Suppressed alert as insufficient time has passed since the last qualified alert message.");
            }
            lockTime = time.AddMinutes(5); //Require no message that would qualify as alert for 5 minutes before next alert
            saveChanges();

            return result;
        }

        [JsonIgnore]
        public string readableFullName => triggerGroup.name;
    }
}