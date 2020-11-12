using AlarmManagerT.Resources;
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
            Title = AppResources.LoginCodePage_Title;
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
                        return AppResources.LoginCodePage_Error_Default;
                }

            }
        }

        public bool ErrorActive => errorStatus != TStatus.OK;
    }
}
