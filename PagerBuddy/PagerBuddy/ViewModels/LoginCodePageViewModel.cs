using PagerBuddy.Resources;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xamarin.Forms;
using static PagerBuddy.Services.ClientExceptions;

namespace PagerBuddy.ViewModels
{
    public class LoginCodePageViewModel : BaseViewModel
    {
        private TStatus errorStatus = TStatus.OK;

        public Command Next { get; set; }
        public Command Return { get; set; }

        public LoginCodePageViewModel()
        {
            Title = AppResources.LoginCodePage_Title;
            Next = new Command(() => commitCode());
            Return = new Command(() => commitCode());
        }

        public StringEventHandler RequestAuthenticate;
        public delegate void StringEventHandler(object sender, string load);

        private void commitCode() {
            string code = CodeText;
            
            if(code.Length != 5 || !Regex.IsMatch(code, "[0-9]{5}")) {
                updateErrorState(TStatus.NO_CODE);
                return;
            }
            setWaitStatus(true);
            RequestAuthenticate.Invoke(this, code);
        }

        public void updateErrorState(TStatus newStatus)
        {
            if(errorStatus != newStatus)
            {
                errorStatus = newStatus;
                OnPropertyChanged(nameof(ErrorText));
                OnPropertyChanged(nameof(ErrorActive));
            }
        }
        public void setWaitStatus(bool isWait) {
            IsBusy = isWait;
        }

        public string CodeText { get; set; }
        public string ErrorText {
            get {
                switch (errorStatus)
                {
                    case TStatus.NO_CODE:
                        return AppResources.LoginCodePage_Error_NoCode;
                    case TStatus.INVALID_CODE:
                        return AppResources.LoginCodePage_Error_InvalidCode;
                    case TStatus.OFFLINE:
                        return AppResources.LoginCodePage_Error_Offline;
                    default:
                        return AppResources.LoginCodePage_Error_Default;
                }

            }
        }

        public bool ErrorActive => errorStatus != TStatus.OK;
    }
}
