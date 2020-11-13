using AlarmManagerT.Resources;
using AlarmManagerT.Services;
using AlarmManagerT.Views;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace AlarmManagerT.ViewModels
{
    class MenuPageViewModel : BaseViewModel
    {
        public Command ViewAccount { get; set; }
        public Command Test { get; set; } //TODO: RBF
        public Command About { get; set; }
        public Command Share { get; set; }
        public Command NotificationSettings { get; set; }
        public Command Logout { get; set; }

        public MenuPageViewModel()
        {
            ViewAccount = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.AccountPage));
            Test = new Command(() => MessagingCenter.Send(this, "TEST")); //TODO: RBF

            NotificationSettings = new Command(() => RequestNotificationSettings.Invoke(this, null));
            About = new Command(() => RequestAbout.Invoke(this, null));
            Share = new Command(() => RequestShare.Invoke(this, null));
            Logout = new Command(() => RequestLogout.Invoke(this, null));

            MessagingCenter.Subscribe<CommunicationService>(this, CommunicationService.MESSAGING_KEYS.USER_DATA_CHANGED.ToString(), (obj) => userDataChanged());
        }

        public NavigationEventHandler RequestNavigation;
        public delegate void NavigationEventHandler(object sender, MenuPage.MENU_PAGE destination);

        public EventHandler RequestNotificationSettings;
        public EventHandler RequestAbout;
        public EventHandler RequestShare;
        public EventHandler RequestLogout;

        private void userDataChanged()
        {
            OnPropertyChanged(nameof(UserName));
            OnPropertyChanged(nameof(UserPhone));
            OnPropertyChanged(nameof(UserHasPic));
            OnPropertyChanged(nameof(ShowDefaultPic));
            OnPropertyChanged(nameof(UserPic));
        }

        public string UserName => DataService.getConfigValue(DataService.DATA_KEYS.USER_NAME, AppResources.MenuPage_UserName_Default);
        public string UserPhone => DataService.getConfigValue(DataService.DATA_KEYS.USER_PHONE, AppResources.MenuPage_UserPhone_Default);
        public bool UserHasPic => DataService.getConfigValue(DataService.DATA_KEYS.USER_HAS_PHOTO, false);

        public bool ShowDefaultPic => !UserHasPic;
        public string UserPic {
            get {
                if (UserHasPic)
                {
                    return DataService.profilePicSavePath(DataService.DATA_KEYS.USER_PHOTO.ToString());
                }
                else
                {
                    return null;
                }
        }
        }
    }
}
