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
            Title = "Login";
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
                        return "The phone number is not associated to a telegram account or too many login attempts have been made. Check the phone number you entered.";
                    default:
                        return "An unknown error occured. Try again later...";
                }

            }
        }

        public bool ErrorActive => errorStatus != TStatus.OK;
    }
}
