using FFImageLoading.Svg.Forms;
using PagerBuddy.Resources;
using PagerBuddy.Services;
using PagerBuddy.Views;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private string iconColor {
            get {
                Style style = (Style)Application.Current.Resources["ActionIcons"];

                string mode = Application.Current.RequestedTheme == OSAppTheme.Dark ? "Dark" : "Light";
                Setter themeSetter = style.Setters.First((setter) => setter.TargetName == mode);
                return ((Color)themeSetter.Value).ToHex();
            }
        }

        public MenuPageViewModel() {

            NotificationSettings = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.NotificationSettings));
            About = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.AboutPage));
            Share = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.Share));
            Logout = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.LogoutUser));
            Profile = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.Login));
            Website = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.Website));

            MessagingCenter.Subscribe<CommunicationService>(this, CommunicationService.MESSAGING_KEYS.USER_DATA_CHANGED.ToString(), (_) => userDataChanged());
            MessagingCenter.Subscribe<MainPage>(this, MainPage.MESSAGING_KEYS.LOGOUT_USER.ToString(), (_) => userDataChanged());

            Application.Current.RequestedThemeChanged += (s, a) => {
                OnPropertyChanged(nameof(AboutPic));
                OnPropertyChanged(nameof(NotificationConfigPic));
                OnPropertyChanged(nameof(SharePic));
                OnPropertyChanged(nameof(WebsitePic));
                OnPropertyChanged(nameof(LogoutPic));
            };
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
                    SvgImageSource source = SvgImageSource.FromResource("PagerBuddy.Resources.Images.user_default.svg");
                    Color accent = (Color) Application.Current.Resources["AccentColor"];
                    source.ReplaceStringMap = new Dictionary<string, string> { { "black", accent.ToHex() } };
                    return source;
                }
            }
        }

        public ImageSource NotificationConfigPic{
            get {
                SvgImageSource source = SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_alert.svg");
                source.ReplaceStringMap = new Dictionary<string, string>() { { "black", iconColor } };
                return source;
            }
        }
        public ImageSource SharePic {
            get {
                SvgImageSource source = SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_share.svg");
                source.ReplaceStringMap = new Dictionary<string, string>() { { "black", iconColor } };
                return source;
            }
        }
        public ImageSource WebsitePic {
            get {
                SvgImageSource source = SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_facebook.svg");
                source.ReplaceStringMap = new Dictionary<string, string>() { { "black", iconColor } };
                return source;
            }
        }
        public ImageSource AboutPic {
            get {
                SvgImageSource source = SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_about.svg");
                source.ReplaceStringMap = new Dictionary<string, string>() { { "black", iconColor } };
                return source;
            }
        }
        public ImageSource LogoutPic {
            get {
                SvgImageSource source = SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_logout.svg");
                source.ReplaceStringMap = new Dictionary<string, string>() { { "black", iconColor } };
                return source;
            }
        }
    }
}
