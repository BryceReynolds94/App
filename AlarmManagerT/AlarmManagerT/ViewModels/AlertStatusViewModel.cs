using AlarmManagerT.Models;
using AlarmManagerT.Resources;
using AlarmManagerT.Services;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace AlarmManagerT.ViewModels
{
    public class AlertStatusViewModel : BaseViewModel
    {
        private AlertConfig alertConfig;

        public Command SnoozeAlert { get; set; }
        public Command EditAlert { get; set; }
        public Command DeleteAlert { get; set; }

        public enum MESSAGING_KEYS { EDIT_ALERT_CONFIG, DELETE_ALERT_CONFIG, REQUEST_SNOOZE_TIME};
        
        public AlertStatusViewModel(AlertConfig alertConfig)
        {
            this.alertConfig = alertConfig;

            EditAlert = new Command(() => MessagingCenter.Send(this, MESSAGING_KEYS.EDIT_ALERT_CONFIG.ToString(), alertConfig));
            DeleteAlert = new Command(() => MessagingCenter.Send(this, MESSAGING_KEYS.DELETE_ALERT_CONFIG.ToString(), alertConfig));
            SnoozeAlert = new Command(() => setSnooze());
        }

        private void setSnooze()
        {
            if (alertConfig.snoozeActive)
            {
                alertConfig.setSnoozeTime(DateTime.MinValue);
                alertPropertiesChanged();
            }
            else
            {
                Action<DateTime> action = new Action<DateTime>((dateTime) =>
                {
                    if (dateTime > DateTime.Now)
                    {
                        alertConfig.setSnoozeTime(dateTime);
                        alertPropertiesChanged();
                    }
                });

                MessagingCenter.Send(this, MESSAGING_KEYS.REQUEST_SNOOZE_TIME.ToString(), action);
            }
        }

        public bool IsActive {
            get => alertConfig.isActive;
            set {
                alertConfig.setActiveState(value);
                alertPropertiesChanged();
            }
        }

        private void alertPropertiesChanged()
        {
            OnPropertyChanged(nameof(StatusFieldPic));
            OnPropertyChanged(nameof(StatusFieldText));
            OnPropertyChanged(nameof(SnoozePic));
            OnPropertyChanged(nameof(SnoozeEnabled));
        }
        public string GroupName => alertConfig.triggerGroup.name;
        public string KeywordText {
            get {
                
                string outString = "";
                switch (alertConfig.triggerType) {
                    case AlertConfig.TRIGGER_TYPE.ANY:
                        outString = AppResources.AlertStatus_Trigger_Any;
                        break;
                    case AlertConfig.TRIGGER_TYPE.SERVER:
                        outString = AppResources.AlertStatus_Trigger_Server;
                        break;
                    case AlertConfig.TRIGGER_TYPE.KEYWORD:
                        outString = alertConfig.triggerKeyword;
                        break;
                }

                if (alertConfig.timeRestriction)
                {
                    outString = outString + Environment.NewLine + alertConfig.activeTimeConfig.getActiveString();
                }
                return outString;
            }
        }
        public string StatusFieldText {
            get {
                if (alertConfig.snoozeActive)
                {
                    return string.Format(AppResources.AlertStatus_Status_Deactivated, alertConfig.snoozeTime);
                }
                else if (alertConfig.lastTriggered > DateTime.MinValue)
                {
                    return alertConfig.lastTriggered.ToString("dd.MM.yyyy HH:mm");
                }
                else
                {
                    return AppResources.AlertStatus_Status_Default;
                }
            }
        }

        public string StatusFieldPic {
            get {
                if (alertConfig.snoozeActive)
                {
                    return "resource://AlarmManagerT.Resources.Images.icon_alert_snooze_inactive.svg";
                }
                else
                {
                    return "resource://AlarmManagerT.Resources.Images.icon_history.svg";
                }
            }
        }

        public bool SnoozeEnabled => alertConfig.isActive;

        public string SnoozePic {
            get {
                if (!alertConfig.isActive)
                {
                    return "resource://AlarmManagerT.Resources.Images.icon_alert_snooze_inactive.svg";
                }
                else if (alertConfig.snoozeActive)
                {
                    return "resource://AlarmManagerT.Resources.Images.icon_alert_snooze_off.svg";
                }
                else
                {
                    return "resource://AlarmManagerT.Resources.Images.icon_alert_snooze.svg";
                }
            }
        }

        public bool ShowDefaultPic => !alertConfig.triggerGroup.hasImage;
        public bool HasPic => alertConfig.triggerGroup.hasImage;

        public string GroupPicPath {
            get {
                if (alertConfig.triggerGroup.hasImage)
                {
                    return DataService.profilePicSavePath(alertConfig.id);
                }
                else
                {
                    return null;
                }
            }
        }

    }
}
