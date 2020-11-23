using PagerBuddy.Resources;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using static PagerBuddy.Services.ClientExceptions;

namespace PagerBuddy.ViewModels {
    public class LoginPasswordPageViewModel : BaseViewModel {

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public Command Return { get; set; }
        public Command Next { get; set; }
        private TStatus errorStatus = TStatus.OK;
        public LoginPasswordPageViewModel() {

            Title = AppResources.LoginPasswordPage_Title;
            Return = new Command(() => RequestAuthenticate.Invoke(this, Password));
            Next = new Command(() => RequestAuthenticate.Invoke(this, Password));

        }

        public StringEventHandler RequestAuthenticate;
        public delegate void StringEventHandler(object sender, string load);

        public string Password { get; set; }

        public void updateErrorState(TStatus newStatus) {
            if (errorStatus != newStatus) {
                errorStatus = newStatus;
                OnPropertyChanged(nameof(ErrorText));
                OnPropertyChanged(nameof(ErrorActive));
            }
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
