using PagerBuddy.Resources;
using PagerBuddy.Services;
using PagerBuddy.Views;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace PagerBuddy.ViewModels
{
    class MenuPageViewModel : BaseViewModel
    {
        public Command About { get; set; }
        public Command Share { get; set; }
        public Command NotificationSettings { get; set; }
        public Command Logout { get; set; }
        public Command Profile { get; set; }

        public MenuPageViewModel()
        {

            NotificationSettings = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.NotificationSettings));
            About = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.AboutPage));
            Share = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.Share));
            Logout = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.LogoutUser));
            Profile = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.Login));

            MessagingCenter.Subscribe<CommunicationService>(this, CommunicationService.MESSAGING_KEYS.USER_DATA_CHANGED.ToString(), (_) => userDataChanged());
            MessagingCenter.Subscribe<MainPage>(this, MainPage.MESSAGING_KEYS.LOGOUT_USER.ToString(), (_) => userDataChanged());
        }

        public NavigationEventHandler RequestNavigation;
        public delegate void NavigationEventHandler(object sender, MenuPage.MENU_PAGE destination);

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
