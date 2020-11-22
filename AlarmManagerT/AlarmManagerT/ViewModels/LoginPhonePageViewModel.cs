using AlarmManagerT.Resources;
using PhoneNumbers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;
using static AlarmManagerT.Services.ClientExceptions;

namespace AlarmManagerT.ViewModels {
    public class LoginPhonePageViewModel : BaseViewModel {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string regionCode;
        private PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();

        private TStatus errorStatus = TStatus.OK;
        public Command Next { get; set; }
        public Command Return { get; set; }

        public LoginPhonePageViewModel() {
            Title = AppResources.LoginPhonePage_Title;
            Next = new Command(() => commitPhoneNumber());
            Return = new Command(() => commitPhoneNumber());

            string cultureName = CultureInfo.CurrentCulture.Name;
            regionCode = cultureName.Substring(cultureName.Length - 2);
        }

        public StringRequestHandler RequestClientLogin;
        public delegate void StringRequestHandler(object sender, string load);

        private void commitPhoneNumber() {
            if (PhoneNumber.StartsWith("+99966") && PhoneNumber.Length == 11) {
                Logger.Info("User entered a Test Number - accepting for test authorisation.");
                RequestClientLogin.Invoke(this, PhoneNumber);
                return;
            }

            PhoneNumber no;
            try {
                no = phoneUtil.Parse(PhoneNumber, regionCode);
            } catch (NumberParseException e) {
                Logger.Warn(e, "Exception while trying to parse user phone input.");
                changeErrorStatus(TStatus.NO_PHONE_NUMBER);
                return;
            }

            if (!phoneUtil.IsValidNumber(no)) {
                changeErrorStatus(TStatus.NO_PHONE_NUMBER);
                return;
            }
            string formattedNo = phoneUtil.Format(no, PhoneNumberFormat.E164);
            PhoneNumber = formattedNo;
            RequestClientLogin.Invoke(this, formattedNo);
        }

        public void changeErrorStatus(TStatus newStatus) {
            if (errorStatus != newStatus) {
                errorStatus = newStatus;
                OnPropertyChanged(nameof(ErrorText));
                OnPropertyChanged(nameof(ErrorActive));
            }
        }

        public string PhoneNumber { get; set; }

        public string PhoneNumberHint {
            get {
                return "+" + phoneUtil.GetCountryCodeForRegion(regionCode);
            }
        }

        public string ErrorText {
            get {
                switch (errorStatus) {
                    case TStatus.NO_PHONE_NUMBER:
                        return AppResources.LoginPhonePage_Error_NoPhoneNumber;
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
