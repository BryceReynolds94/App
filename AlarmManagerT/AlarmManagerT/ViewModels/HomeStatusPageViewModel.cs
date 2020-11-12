using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

using Xamarin.Forms;

using AlarmManagerT.Models;
using AlarmManagerT.Views;
using AlarmManagerT.Services;
using TeleSharp.TL.Upload;
using System.Collections.Generic;
using System.Data;
using AlarmManagerT.Resources;

namespace AlarmManagerT.ViewModels
{
    public class HomeStatusPageViewModel : BaseViewModel
    {
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

        public HomeStatusPageViewModel(Collection<AlertConfig> rawAlertList){
            Title = AppResources.HomeStatusPage_Title;
            fillAlertList(rawAlertList);

            ToggleAllActive = new Command(() => updateAllActiveState());
            ToggleAllSnooze = new Command(() => updateAllSnoozeState());
            AddConfig = new Command(() => AddConfigurationRequest.Invoke(this, null));

            ErrorAction = new Command(() => ErrorActionClicked());
            WarningAction = new Command(() => WarningActionClicked());

        }

        public void fillAlertList(Collection<AlertConfig> alertConfigs)
        {
            alertList = new ObservableCollection<AlertStatusViewModel>();

            foreach(AlertConfig config in alertConfigs)
            {
                alertList.Add(new AlertStatusViewModel(config));
            }
            OnPropertyChanged(nameof(alertList));
        }

        public void setWarningState(bool allDeactivated, bool allSnoozed)
        {
            if (allDeactivated)
            {
                warningState = WARNING_ACTION.DEACTIVATED;
            }else if (allSnoozed)
            {
                warningState = WARNING_ACTION.SNOOZE_SET;
            }
            else
            {
                warningState = WARNING_ACTION.NONE;
            }
            OnPropertyChanged(nameof(WarningText));
            OnPropertyChanged(nameof(WarningActive));
            OnPropertyChanged(nameof(ToggleAllActiveIcon));
            OnPropertyChanged(nameof(ToggleAllSnoozeIcon));
            OnPropertyChanged(nameof(ToggleSnoozeEnabled));
        }

        public void setErrorState(bool hasInternet, bool isAuthorised)
        {
            if (!hasInternet)
            {
                errorState = ERROR_ACTION.NO_INTERNET;
            }else if (!isAuthorised)
            {
                errorState = ERROR_ACTION.NO_TELEGRAM;
            }
            else
            {
                errorState = ERROR_ACTION.NONE;
            }
            OnPropertyChanged(nameof(ErrorText));
            OnPropertyChanged(nameof(ErrorActive));
            OnPropertyChanged(nameof(AddEnabled));
        }

        private void updateAllActiveState()
        {
            if(warningState == WARNING_ACTION.DEACTIVATED)
            {
                warningState = WARNING_ACTION.NONE;
                AllDeactivatedStateChanged.Invoke(this, false);
            }
            else
            {
                warningState = WARNING_ACTION.DEACTIVATED;
                AllDeactivatedStateChanged.Invoke(this, true);
            }
            OnPropertyChanged(nameof(WarningText));
            OnPropertyChanged(nameof(WarningActive));
            OnPropertyChanged(nameof(ToggleAllActiveIcon));
            OnPropertyChanged(nameof(ToggleAllSnoozeIcon));
            OnPropertyChanged(nameof(ToggleSnoozeEnabled));
        }

        private async void updateAllSnoozeState()
        {
            if(warningState == WARNING_ACTION.SNOOZE_SET)
            {
                warningState = WARNING_ACTION.NONE;
                AllSnoozeStateChanged.Invoke(this, DateTime.MinValue);
            }
            else
            {
                DateTime snoozeTime = await RequestSnoozeTime.Invoke(this, AppResources.HomeStatusPage_Snooze_Prompt);
                if (snoozeTime > DateTime.Now)
                {
                    warningState = WARNING_ACTION.SNOOZE_SET;
                    AllSnoozeStateChanged.Invoke(this, snoozeTime);
                }
            }
            OnPropertyChanged(nameof(WarningText));
            OnPropertyChanged(nameof(WarningActive));
            OnPropertyChanged(nameof(ToggleAllSnoozeIcon));
        }

        private void ErrorActionClicked()
        {
            if (errorState == ERROR_ACTION.NO_TELEGRAM)
            {
                RequestLogin.Invoke(this, null);
            }
            else if (errorState == ERROR_ACTION.NO_INTERNET)
            {
                RequestRefresh.Invoke(this, null);
            }

        }

        private void WarningActionClicked()
        {
            if (warningState == WARNING_ACTION.SNOOZE_SET)
            {
                updateAllSnoozeState();
            }
            else if (warningState == WARNING_ACTION.DEACTIVATED)
            {
                updateAllActiveState();
            }

        }

        public string ErrorText {
            get {
                switch (errorState)
                {
                    case ERROR_ACTION.NO_INTERNET:
                        return AppResources.HomeStatusPage_Error_NoInternet;
                    case ERROR_ACTION.NO_TELEGRAM:
                        return AppResources.HomeStatusPage_Error_NoTelegram;
                    default:
                        return "";

                }
            }
        }

        public string WarningText {
            get {
                switch (warningState)
                {
                    case WARNING_ACTION.SNOOZE_SET:
                        DateTime snoozeTime = new DateTime(DataService.getConfigValue(DataService.DATA_KEYS.CONFIG_SNOOZE_ALL, DateTime.Now.Ticks));
                        return string.Format(AppResources.HomeStatusPage_Warning_Snooze, snoozeTime); //TODO: Check if format string works
                    case WARNING_ACTION.DEACTIVATED:
                        return AppResources.HomeStatusPage_Warning_Deactivated;
                    default:
                        return "";

                }
            }
        }

        public bool ErrorActive {
            get => errorState != ERROR_ACTION.NONE;
        }

        public bool WarningActive {
            get => warningState != WARNING_ACTION.NONE;
        }

        public string ToggleAllActiveIcon {
            get {
                if(warningState == WARNING_ACTION.DEACTIVATED)
                {
                    return "resource://AlarmManagerT.Images.icon_alert_off.svg";
                }
                else
                {
                    return "resource://AlarmManagerT.Images.icon_alert.svg";
                }
            }
        }

        public string ToggleAllSnoozeIcon {
            get {
                switch (warningState)
                {
                    case WARNING_ACTION.SNOOZE_SET:
                        return "resource://AlarmManagerT.Images.icon_alert_snooze_off.svg";
                    case WARNING_ACTION.DEACTIVATED:
                        return "resource://AlarmManagerT.Images.icon_alert_snooze_inactive.svg";
                    default:
                        return "resource://AlarmManagerT.Images.icon_alert_snooze.svg";
                }
            }
         }

        public bool ToggleSnoozeEnabled {
            get => warningState != WARNING_ACTION.DEACTIVATED;
        }

        public bool AddEnabled {
            get => errorState == ERROR_ACTION.NONE;
        }

    }
}