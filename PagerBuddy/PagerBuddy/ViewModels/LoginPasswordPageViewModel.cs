using PagerBuddy.Resources;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using static PagerBuddy.Services.ClientExceptions;

namespace PagerBuddy.ViewModels {
    public class LoginPasswordPageViewModel : BaseViewModel {

        public Command Return { get; set; }
        public Command Next { get; set; }
        private TStatus errorStatus = TStatus.OK;

        public LoginPasswordPageViewModel() {

            Title = AppResources.LoginPasswordPage_Title;
            Return = new Command(() => requestAuthentication());
            Next = new Command(() => requestAuthentication());

        }

        public StringEventHandler RequestAuthenticate;
        public delegate void StringEventHandler(object sender, string load);

        public string Password { get; set; }

        private void requestAuthentication() {
            setWaitStatus(true);
            RequestAuthenticate.Invoke(this, Password);
        }

        public void updateErrorState(TStatus newStatus) {
            if (errorStatus != newStatus) {
                errorStatus = newStatus;
                OnPropertyChanged(nameof(ErrorText));
                OnPropertyChanged(nameof(ErrorActive));
            }
        }

        public void setWaitStatus(bool isWait) {
            IsBusy = isWait;
        }

        public string ErrorText {
            get {
                switch (errorStatus) {
                    case TStatus.INVALID_PASSWORD:
                        return AppResources.LoginPasswordPage_Error_InvalidPassword;
                    default:
                        return AppResources.LoginPasswordPage_Error_Default;
                }

            }
        }

        public bool ErrorActive => errorStatus != TStatus.OK;

    }
}
