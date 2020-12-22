﻿using FFImageLoading.Svg.Forms;
using PagerBuddy.Models;
using PagerBuddy.Resources;
using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace PagerBuddy.ViewModels {
    public class AlertStatusViewModel : BaseViewModel {
        private AlertConfig alertConfig;

        public Command SnoozeAlert { get; set; }
        public Command EditAlert { get; set; }
        public Command DeleteAlert { get; set; }

        public enum MESSAGING_KEYS { EDIT_ALERT_CONFIG, DELETE_ALERT_CONFIG, REQUEST_SNOOZE_TIME };

        public AlertStatusViewModel(AlertConfig alertConfig) {
            this.alertConfig = alertConfig;

            EditAlert = new Command(() => MessagingCenter.Send(this, MESSAGING_KEYS.EDIT_ALERT_CONFIG.ToString(), alertConfig));
            DeleteAlert = new Command(() => MessagingCenter.Send(this, MESSAGING_KEYS.DELETE_ALERT_CONFIG.ToString(), alertConfig));
            SnoozeAlert = new Command(() => setSnooze());
        }

        private void setSnooze() {
            if (alertConfig.snoozeActive) {
                alertConfig.setSnoozeTime(DateTime.MinValue);
                alertPropertiesChanged();
            } else {
                Action<DateTime> action = new Action<DateTime>((dateTime) => {
                    if (dateTime > DateTime.Now) {
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

        private void alertPropertiesChanged() {
            OnPropertyChanged(nameof(StatusFieldPic));
            OnPropertyChanged(nameof(StatusFieldText));
            OnPropertyChanged(nameof(SnoozePic));
            OnPropertyChanged(nameof(SnoozeEnabled));
        }
        public string GroupName => alertConfig.triggerGroup.name;
        public string KeywordText {
            get {

                string outString = string.Empty;
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

                if (alertConfig.timeRestriction) {
                    outString = outString + Environment.NewLine + alertConfig.activeTimeConfig.getActiveString();
                }
                return outString;
            }
        }
        public string StatusFieldText {
            get {
                if (alertConfig.snoozeActive) {
                    return string.Format(AppResources.AlertStatus_Status_Deactivated, alertConfig.snoozeTime);
                } else if (alertConfig.lastTriggered > DateTime.MinValue) {
                    return alertConfig.lastTriggered.ToString("dd.MM.yyyy HH:mm");
                } else {
                    return AppResources.AlertStatus_Status_Default;
                }
            }
        }

        public ImageSource TriggerPic => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_trigger.svg");

        public ImageSource StatusFieldPic {
            get {
                if (alertConfig.snoozeActive) {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert_snooze_inactive.svg");
                } else {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_history.svg");
                }
            }
        }

        public bool SnoozeEnabled => alertConfig.isActive;
        public ImageSource SnoozePic {
            get {
                if (!alertConfig.isActive) {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert_snooze_inactive.svg");
                } else if (alertConfig.snoozeActive) {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert_snooze_off.svg");
                } else {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert_snooze.svg");
                }
            }
        }

        public ImageSource GroupPic {
            get {
                if (alertConfig.triggerGroup.hasImage) {
                    return ImageSource.FromFile(DataService.profilePicSavePath(alertConfig.id));
                } else {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.group_default.svg");
                }
            }
        }

        public ImageSource EditPic => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_settings.svg");
        public ImageSource DeletePic => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_delete.svg");

    }
}
