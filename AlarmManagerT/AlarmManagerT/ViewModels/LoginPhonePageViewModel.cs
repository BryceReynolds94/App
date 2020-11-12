using AlarmManagerT.Resources;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using static AlarmManagerT.Services.ClientExceptions;

namespace AlarmManagerT.ViewModels
{
    public class LoginPhonePageViewModel : BaseViewModel
    {

        private TStatus errorStatus = TStatus.OK;
        public Command Next { get; set; }

        public LoginPhonePageViewModel()
        {
            Title = AppResources.LoginPhonePage_Title;
            Next = new Command(() => RequestClientLogin.Invoke(this, PhoneNumber));
        }

        public StringRequestHandler RequestClientLogin;
        public delegate void StringRequestHandler(object sender, string load);

        public void changeErrorStatus(TStatus newStatus)
        {
            if (errorStatus != newStatus)
            {
                errorStatus = newStatus;
                OnPropertyChanged(nameof(ErrorText));
                OnPropertyChanged(nameof(ErrorActive));
            }
        }

        public string PhoneNumber { get; set; }

        public string ErrorText {
            get {
                switch (errorStatus)
                {
                    case TStatus.INVALID_PHONE_NUMBER:
                        return AppResources.LoginPhonePage_Error_InvalidPhoneNumber;
                    default:
                        return AppResources.LoginPhonePage_Error_Default;
                }

            }
        }

        public bool ErrorActive => errorStatus != TStatus.OK;
    }
}
