using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

using Xamarin.Forms;

using PagerBuddy.Models;
using PagerBuddy.Views;
using PagerBuddy.Services;
using System.Collections.Generic;
using System.Data;
using PagerBuddy.Resources;
using FFImageLoading.Svg.Forms;

namespace PagerBuddy.ViewModels {
    public class HomeStatusPageViewModel : BaseViewModel {
        public ObservableCollection<AlertStatusViewModel> alertList { get; set; }

        private enum ERROR_ACTION { NO_INTERNET, NO_TELEGRAM, NONE };

        private ERROR_ACTION errorState = ERROR_ACTION.NO_INTERNET;
        private bool allDeactivated = false;
        private bool allSnoozed = false;

        public Command DeactivateAll { get; set; }
        public Command DeactivateAllOff { get; set; }
        public Command SnoozeAll { get; set; }
        public Command SnoozeAllOff { get; set; }
        public Command ReloadConfig { get; set; } //TODO: Add this to UI
        public Command Login { get; set; }

        public EventHandler RefreshConfigurationRequest;
        public UpdateStatusEventHandler AllDeactivatedStateChanged;
        public UpdateSnoozeEventHandler AllSnoozeStateChanged;
        public RequestSnoozeEventHandler RequestSnoozeTime;
        public EventHandler RequestLogin;

        public delegate void UpdateStatusEventHandler(object sender, bool newStatus);
        public delegate void UpdateSnoozeEventHandler(object sender, DateTime snoozeTime);
        public delegate Task<DateTime> RequestSnoozeEventHandler(object sender, EventArgs args);

        public HomeStatusPageViewModel() {
            Title = AppResources.HomeStatusPage_Title;
            fillAlertList(new Collection<AlertConfig>());

            DeactivateAll = new Command(() => setDeactivateState(true));
            DeactivateAllOff = new Command(() => setDeactivateState(false));
            SnoozeAll = new Command(() => _ = setSnoozeState(true));
            SnoozeAllOff = new Command(() => _ = setSnoozeState(false));
            ReloadConfig = new Command(() => reloadConfig());
            Login = new Command(() => RequestLogin.Invoke(this, null));

        }

        private void reloadConfig() {
            IsBusy = true;
            RefreshConfigurationRequest.Invoke(this, null);
        }

        public void setDeactivateState(bool state, bool init = false) {
            allDeactivated = state;
            if (!init) {
                AllDeactivatedStateChanged.Invoke(this, state);
            }
            OnPropertyChanged(nameof(WarningDeactivate));
            OnPropertyChanged(nameof(AllDeactivateIcon));
        }

        public async Task setSnoozeState(bool state, bool init = false) {
            DateTime snoozeTime = DateTime.MinValue;
            if (state && !init) {
                snoozeTime = await RequestSnoozeTime.Invoke(this, null);
                if(snoozeTime < DateTime.Now) {
                    return;
                }
            }
            allSnoozed = state;
            if (!init) {
                AllSnoozeStateChanged.Invoke(this, snoozeTime);
            }
            OnPropertyChanged(nameof(WarningSnooze));
            OnPropertyChanged(nameof(AllSnoozeIcon));
        }

        public void fillAlertList(Collection<AlertConfig> alertConfigs) {
            alertList = new ObservableCollection<AlertStatusViewModel>();

            foreach (AlertConfig config in alertConfigs) {
                alertList.Add(new AlertStatusViewModel(config));
            }
            OnPropertyChanged(nameof(alertList));
            IsBusy = false;
        }

        public void setErrorState(bool hasInternet, bool isAuthorised) {
            if (!hasInternet) {
                errorState = ERROR_ACTION.NO_INTERNET;
            } else if (!isAuthorised) {
                errorState = ERROR_ACTION.NO_TELEGRAM;
            } else {
                errorState = ERROR_ACTION.NONE;
            }
            OnPropertyChanged(nameof(ConfigActive));
            OnPropertyChanged(nameof(ReloadConfigEnabled));
            OnPropertyChanged(nameof(ReloadConfigIcon));
            OnPropertyChanged(nameof(ErrorLogin));
        }

        public bool ConfigActive => errorState != ERROR_ACTION.NO_TELEGRAM;

        public bool WarningDeactivate => allDeactivated;
        public ImageSource WarningDeactivateIcon => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert_off.svg");
        public ImageSource AllDeactivateIcon {
            get {
                if (allDeactivated) {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert.svg");
                } else {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert_off.svg");
                }
            }
        }
        public bool WarningSnooze => allSnoozed;
        public ImageSource WarningSnoozeIcon => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert_snooze.svg");
        public ImageSource AllSnoozeIcon {
            get {
                if (allSnoozed) {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert_snooze_off.svg");
                } else {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert_snooze.svg");
                }
            }
        }

        public bool ReloadConfigEnabled => errorState == ERROR_ACTION.NONE;
        public ImageSource ReloadConfigIcon {
            get {
                if (ReloadConfigEnabled) {  //TODO: Change Icons
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_add.svg");
                } else {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_add_inactive.svg");
                }
            }
        }

        public bool ErrorLogin => errorState == ERROR_ACTION.NO_TELEGRAM;
        public ImageSource LoginIcon => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_login.svg");

        public bool EmptyList => alertList.Count == 0;
        public ImageSource EmptyTabIcon => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_add_light.svg"); //TODO: Replace Icon

    }
}