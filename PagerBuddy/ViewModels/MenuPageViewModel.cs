using FFImageLoading.Svg.Forms;
using PagerBuddy.Resources;
using PagerBuddy.Services;
using PagerBuddy.Views;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace PagerBuddy.ViewModels {
    class MenuPageViewModel : BaseViewModel {
        public Command About { get; set; }
        public Command Share { get; set; }
        public Command NotificationSettings { get; set; }
        public Command Logout { get; set; }
        public Command Profile { get; set; }
        public Command Website { get; set; }

        public MenuPageViewModel() {

            NotificationSettings = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.NotificationSettings));
            About = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.AboutPage));
            Share = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.Share));
            Logout = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.LogoutUser));
            Profile = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.Login));
            Website = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.Website));

            MessagingCenter.Subscribe<CommunicationService>(this, CommunicationService.MESSAGING_KEYS.USER_DATA_CHANGED.ToString(), (_) => userDataChanged());
            MessagingCenter.Subscribe<MainPage>(this, MainPage.MESSAGING_KEYS.LOGOUT_USER.ToString(), (_) => userDataChanged());
        }

        public NavigationEventHandler RequestNavigation;
        public delegate void NavigationEventHandler(object sender, MenuPage.MENU_PAGE destination);

        private void userDataChanged() {
            OnPropertyChanged(nameof(UserName));
            OnPropertyChanged(nameof(UserPhone));
            OnPropertyChanged(nameof(UserPic));
        }

        public string UserName => DataService.getConfigValue(DataService.DATA_KEYS.USER_NAME, AppResources.MenuPage_UserName_Default);
        public string UserPhone => DataService.getConfigValue(DataService.DATA_KEYS.USER_PHONE, AppResources.MenuPage_UserPhone_Default);

        public ImageSource UserPic {
            get {
                if (DataService.getConfigValue(DataService.DATA_KEYS.USER_HAS_PHOTO, false)) {
                    return ImageSource.FromFile(DataService.profilePicSavePath(DataService.DATA_KEYS.USER_PHOTO.ToString()));
                } else {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.user_default.svg");
                }
            }
        }

        public ImageSource NotificationConfigPic => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert.svg");
        public ImageSource SharePic => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_share.svg");
        public ImageSource WebsitePic => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_facebook.svg");
        public ImageSource AboutPic => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_about.svg");
        public ImageSource LogoutPic => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_logout.svg");
    }
}
