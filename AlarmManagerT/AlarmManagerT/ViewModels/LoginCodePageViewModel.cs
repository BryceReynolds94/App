using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using static AlarmManagerT.Services.ClientExceptions;

namespace AlarmManagerT.ViewModels
{
    public class LoginCodePageViewModel : BaseViewModel
    {
        private TStatus errorStatus = TStatus.OK;

        public Command Next { get; set; }

        public LoginCodePageViewModel()
        {
            Title = "Login";
            Next = new Command(() => RequestAuthenticate.Invoke(this, CodeText));
        }

        public StringEventHandler RequestAuthenticate;
        public delegate void StringEventHandler(object sender, string load);

        public void updateErrorState(TStatus newStatus)
        {
            if(errorStatus != newStatus)
            {
                errorStatus = newStatus;
                OnPropertyChanged(nameof(ErrorText));
                OnPropertyChanged(nameof(ErrorActive));
            }
        }

        public string CodeText { get; set; }
        public string ErrorText {
            get {
                switch (errorStatus)
                {
                    default:
                        return "An unknown error occured. Try again later...";
                }

            }
        }

        public bool ErrorActive => errorStatus != TStatus.OK;
    }
}
