﻿using PagerBuddy.Resources;
using PhoneNumbers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;
using static PagerBuddy.Services.ClientExceptions;

namespace PagerBuddy.ViewModels {
    public class LoginPhonePageViewModel : BaseViewModel {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string regionCode;
        private readonly PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();

        private TStatus errorStatus = TStatus.OK;

        public Dictionary<string, string> colorSetAccent {
            get {
                Color accent = (Color)Application.Current.Resources["AccentColor"];
                return new Dictionary<string, string>() { { "black", accent.ToHex() } };
            }
        }
        public Command Next { get; set; }
        public Command Return { get; set; }

        public Command Hyperlink { get; set; }

        public LoginPhonePageViewModel() {
            Title = AppResources.LoginPhonePage_Title;
            Next = new Command(() => commitPhoneNumber());
            Return = new Command(() => commitPhoneNumber());
            Hyperlink = new Command(() => RequestTelegramLink.Invoke(this, null));

            string cultureName = CultureInfo.CurrentCulture.Name;
            regionCode = cultureName[^2..];
        }

        public EventHandler RequestTelegramLink;
        public StringRequestHandler RequestClientLogin;
        public delegate void StringRequestHandler(object sender, string load);

        private void commitPhoneNumber() {

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
            setWaitStatus(true);
            RequestClientLogin.Invoke(this, formattedNo);
        }

        public void setWaitStatus(bool isWait) {
            IsBusy = isWait;
        }

        public void changeErrorStatus(TStatus newStatus) {
            if (errorStatus != newStatus) {
                errorStatus = newStatus;
                OnPropertyChanged(nameof(ErrorText));
                OnPropertyChanged(nameof(ErrorActive));
            }
        }

        public string PhoneNumber { get; set; }
         
        public bool IsTelegramNotInstalled {
            get {
                Interfaces.INavigation navigation = DependencyService.Get<Interfaces.INavigation>();
                return !navigation.isTelegramInstalled();
            }
        }

        public string PhoneNumberHint => "+" + phoneUtil.GetCountryCodeForRegion(regionCode);

        public string ErrorText {
            get {
                return errorStatus switch {
                    TStatus.NO_PHONE_NUMBER => AppResources.LoginPhonePage_Error_NoPhoneNumber,
                    TStatus.INVALID_PHONE_NUMBER => AppResources.LoginPhonePage_Error_InvalidPhoneNumber,
                    TStatus.OFFLINE => AppResources.LoginPhonePage_Error_Offline,
                    _ => AppResources.LoginPhonePage_Error_Default,
                };
            }
        }

        public bool ErrorActive => errorStatus != TStatus.OK;
    }
}
