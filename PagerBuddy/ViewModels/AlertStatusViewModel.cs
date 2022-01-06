using FFImageLoading.Svg.Forms;
using PagerBuddy.Models;
using PagerBuddy.Resources;
using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PagerBuddy.ViewModels {
    public class AlertStatusViewModel : BaseViewModel {
        private readonly AlertConfig alertConfig;

        public enum MESSAGING_KEYS { ALERT_CONFIG_CHANGED}

        public AlertStatusViewModel(AlertConfig alertConfig) {
            this.alertConfig = alertConfig;
        }

        public bool IsActive {
            get => alertConfig.isActive;
            set {
                if (value != alertConfig.isActive) {
                    alertConfig.setActiveState(value);
                    MessagingCenter.Send(this, MESSAGING_KEYS.ALERT_CONFIG_CHANGED.ToString());
                }
            }
        }

        public string GroupName => alertConfig.triggerGroup.name;
    
        public string StatusFieldText {
            get {
                if (alertConfig.lastTriggered > DateTime.MinValue) {
                    return alertConfig.lastTriggered.ToString("dd.MM.yyyy HH:mm");
                } else {
                    return AppResources.AlertStatus_Status_Default;
                }
            }
        }


        public ImageSource StatusFieldPic {
            get {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_history.svg");
                }
            }


        public ImageSource GroupPic {
            get {
                if (alertConfig.triggerGroup.hasImage) {
                    return ImageSource.FromFile(DataService.profilePicSavePath(alertConfig.id));
                } else {
                    SvgImageSource source = SvgImageSource.FromResource("PagerBuddy.Resources.Images.group_default.svg");
                    Color accent = (Color)Application.Current.Resources["AccentColor"];
                    source.ReplaceStringMap = new Dictionary<string, string>() { { "black", accent.ToHex() } };
                    return source;
                }
            }
        }

    }
}
