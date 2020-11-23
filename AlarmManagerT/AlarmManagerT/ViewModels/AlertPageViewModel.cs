using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace PagerBuddy.ViewModels
{
    public class AlertPageViewModel : BaseViewModel
    {
        public Command Cancel { get; set; }
        public Command Confirm { get; set; }

        public AlertPageViewModel(string alertTitle, string alertText, string alertID, bool hasPic)
        {
            setAlertInfo(alertTitle, alertText);

            if (hasPic) {
                setAlertGroupPic(alertID);
            }

            Cancel = new Command(() => RequestCancel.Invoke(this, null));
            Confirm = new Command(() => RequestConfirm.Invoke(this, null));
        }

        public EventHandler RequestCancel;
        public EventHandler RequestConfirm;

        private void setAlertInfo(string alertTitle, string alertText)
        {
            AlertTitle = alertTitle;
            AlertText = alertText;

            OnPropertyChanged(nameof(AlertText));
            OnPropertyChanged(nameof(AlertTitle));
        }

        private void setAlertGroupPic(string alertID)
        {
            GroupPic = DataService.profilePicSavePath(alertID);
            ShowCustomPic = true;
            OnPropertyChanged(nameof(ShowDefaultPic));
            OnPropertyChanged(nameof(ShowCustomPic));
            OnPropertyChanged(nameof(GroupPic));
        }

        public bool ShowDefaultPic => !ShowCustomPic;
        public bool ShowCustomPic { get; private set; } = false;
        public string GroupPic { get; private set; } = null;
        public string AlertText { get; private set; }
        public string AlertTitle { get; private set; }


    }
}
