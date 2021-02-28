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
        private enum WARNING_ACTION { DEACTIVATED, SNOOZE_SET, NONE };

        private ERROR_ACTION errorState = ERROR_ACTION.NONE;
        private WARNING_ACTION warningState = WARNING_ACTION.NONE;
        public Command ToggleAllActive { get; set; }
        public Command ToggleAllSnooze { get; set; }
        public Command AddConfig { get; set; }
        public Command ErrorAction { get; set; }
        public Command WarningAction { get; set; }

        public EventHandler AddConfigurationRequest;
        public UpdateStatusEventHandler AllDeactivatedStateChanged;
        public UpdateSnoozeEventHandler AllSnoozeStateChanged;
        public HomeStatusPage.SnoozeTimeHandler RequestSnoozeTime;
        public EventHandler RequestLogin;
        public EventHandler RequestRefresh;

        public delegate void UpdateStatusEventHandler(object sender, bool newStatus);
        public delegate void UpdateSnoozeEventHandler(object sender, DateTime snoozeTime);

        public HomeStatusPageViewModel() {
            Title = AppResources.HomeStatusPage_Title;
            fillAlertList(new Collection<AlertConfig>());

            ToggleAllActive = new Command(() => updateAllActiveState());
            ToggleAllSnooze = new Command(() => updateAllSnoozeState());
            AddConfig = new Command(() => AddConfigurationRequest.Invoke(this, null));

            ErrorAction = new Command(() => ErrorActionClicked());
            WarningAction = new Command(() => WarningActionClicked());

        }

        public void fillAlertList(Collection<AlertConfig> alertConfigs) {
            alertList = new ObservableCollection<AlertStatusViewModel>();

            foreach (AlertConfig config in alertConfigs) {
                alertList.Add(new AlertStatusViewModel(config));
            }
            OnPropertyChanged(nameof(alertList));
        }

        public void setWarningState(bool allDeactivated, bool allSnoozed) {
            if (allDeactivated) {
                warningState = WARNING_ACTION.DEACTIVATED;
            } else if (allSnoozed) {
                warningState = WARNING_ACTION.SNOOZE_SET;
            } else {
                warningState = WARNING_ACTION.NONE;
            }
            OnPropertyChanged(nameof(WarningText));
            OnPropertyChanged(nameof(WarningActive));
            OnPropertyChanged(nameof(ToggleAllActiveIcon));
            OnPropertyChanged(nameof(ToggleAllSnoozeIcon));
            OnPropertyChanged(nameof(ToggleAllSnoozeEnabled));
        }

        public void setErrorState(bool hasInternet, bool isAuthorised) {
            if (!hasInternet) {
                errorState = ERROR_ACTION.NO_INTERNET;
            } else if (!isAuthorised) {
                errorState = ERROR_ACTION.NO_TELEGRAM;
            } else {
                errorState = ERROR_ACTION.NONE;
            }
            OnPropertyChanged(nameof(ErrorText));
            OnPropertyChanged(nameof(ErrorActive));
            OnPropertyChanged(nameof(AddConfigEnabled));
            OnPropertyChanged(nameof(AddConfigIcon));
        }

        public void setLoadingState(bool isLoading) {
            IsBusy = isLoading;
        }

        private void updateAllActiveState() {
            if (warningState == WARNING_ACTION.DEACTIVATED) {
                warningState = WARNING_ACTION.NONE;
                AllDeactivatedStateChanged.Invoke(this, false);
            } else {
                warningState = WARNING_ACTION.DEACTIVATED;
                AllDeactivatedStateChanged.Invoke(this, true);
            }
            OnPropertyChanged(nameof(WarningText));
            OnPropertyChanged(nameof(WarningActive));
            OnPropertyChanged(nameof(ToggleAllActiveIcon));
            OnPropertyChanged(nameof(ToggleAllSnoozeIcon));
            OnPropertyChanged(nameof(ToggleAllSnoozeEnabled));
        }

        private async void updateAllSnoozeState() {
            if (warningState == WARNING_ACTION.SNOOZE_SET) {
                warningState = WARNING_ACTION.NONE;
                AllSnoozeStateChanged.Invoke(this, DateTime.MinValue);
            } else {
                DateTime snoozeTime = await RequestSnoozeTime.Invoke(this, AppResources.HomeStatusPage_Snooze_Prompt);
                if (snoozeTime > DateTime.Now) {
                    warningState = WARNING_ACTION.SNOOZE_SET;
                    AllSnoozeStateChanged.Invoke(this, snoozeTime);
                }
            }
            OnPropertyChanged(nameof(WarningText));
            OnPropertyChanged(nameof(WarningActive));
            OnPropertyChanged(nameof(ToggleAllSnoozeIcon));
        }

        private void ErrorActionClicked() {
            if (errorState == ERROR_ACTION.NO_TELEGRAM) {
                RequestLogin.Invoke(this, null);
            } else if (errorState == ERROR_ACTION.NO_INTERNET) {
                RequestRefresh.Invoke(this, null);
            }

        }

        private void WarningActionClicked() {
            if (warningState == WARNING_ACTION.SNOOZE_SET) {
                updateAllSnoozeState();
            } else if (warningState == WARNING_ACTION.DEACTIVATED) {
                updateAllActiveState();
            }

        }


        public string ErrorText {
            get {
                return errorState switch {
                    ERROR_ACTION.NO_INTERNET => AppResources.HomeStatusPage_Error_NoInternet,
                    ERROR_ACTION.NO_TELEGRAM => AppResources.HomeStatusPage_Error_NoTelegram,
                    _ => "",
                };
            }
        }

        public string WarningText {
            get {
                switch (warningState) {
                    case WARNING_ACTION.SNOOZE_SET:
                        DateTime snoozeTime = DataService.getConfigValue(DataService.DATA_KEYS.CONFIG_SNOOZE_ALL, DateTime.Now);
                        return string.Format(AppResources.HomeStatusPage_Warning_Snooze, snoozeTime);
                    case WARNING_ACTION.DEACTIVATED:
                        return AppResources.HomeStatusPage_Warning_Deactivated;
                    default:
                        return "";

                }
            }
        }

        public bool ErrorActive => errorState != ERROR_ACTION.NONE;
        public bool WarningActive => warningState != WARNING_ACTION.NONE;


        public ImageSource ToggleAllActiveIcon {
            get {
                if (warningState == WARNING_ACTION.DEACTIVATED) {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert_off.svg");
                } else {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert.svg");
                }
            }
        }
        public bool ToggleAllSnoozeEnabled => warningState != WARNING_ACTION.DEACTIVATED;
        public ImageSource ToggleAllSnoozeIcon {
            get {
                return warningState switch {
                    WARNING_ACTION.SNOOZE_SET => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert_snooze_off.svg"),
                    WARNING_ACTION.DEACTIVATED => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert_snooze_inactive.svg"),
                    _ => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert_snooze.svg"),
                };
            }
        }

        public bool AddConfigEnabled => errorState == ERROR_ACTION.NONE;
        public ImageSource AddConfigIcon {
            get {
                if (AddConfigEnabled) { 
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_add.svg");
                } else {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_add_inactive.svg");
                }
            }
        }

        public ImageSource AddConfigTabIcon => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_add_light.svg");

    }
}