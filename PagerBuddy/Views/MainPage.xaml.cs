using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using PagerBuddy.Models;
using PagerBuddy.Services;
using PagerBuddy.Resources;
using Xamarin.Essentials;

namespace PagerBuddy.Views {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : FlyoutPage {
        private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public CommunicationService client;

        public enum MESSAGING_KEYS { LOGOUT_USER };
        public MainPage() {
            InitializeComponent();

            FlyoutLayoutBehavior = FlyoutLayoutBehavior.Popover;
            client = new CommunicationService();

            Flyout = new MenuPage();

            Detail = new NavigationPage(new HomeStatusPage(client));
            Detail.Style = (Style) Application.Current.Resources["NavigationBarStyle"];

            MessagingCenter.Subscribe<AboutPage>(this, AboutPage.MESSAGING_KEYS.RESTART_CLIENT.ToString(), async (sender) => await client.forceReloadConnection());
        }

        public async Task NavigateFromMenu(MenuPage.MENU_PAGE destination) {
            switch (destination) {
                case MenuPage.MENU_PAGE.NotificationSettings:
                    requestNotificationSettings();
                    break;
                case MenuPage.MENU_PAGE.Share:
                    await requestShare();
                    break;
                case MenuPage.MENU_PAGE.AboutPage:
                    await requestAboutPageNavigation();
                    break;
                case MenuPage.MENU_PAGE.LogoutUser:
                    await LogoutUser();
                    break;
                case MenuPage.MENU_PAGE.Login:
                    await LoginUser();
                    break;
                case MenuPage.MENU_PAGE.Website:
                    await requestWebsite();
                    break;
                case MenuPage.MENU_PAGE.Close:
                    IsPresented = false;
                    break;
                default:
                    break;
            }
        }

        private async Task LoginUser() {
            if (client.clientStatus == CommunicationService.STATUS.OFFLINE || client.clientStatus == CommunicationService.STATUS.NEW || client.clientStatus == CommunicationService.STATUS.AUTHORISED) {
                return;
            }
            await Detail.Navigation.PushAsync(new LoginPhonePage(client));
            IsPresented = false;
        }

        private async Task LogoutUser() {
            bool result = await DisplayAlert(AppResources.MenuPage_LogoutPrompt_Title, AppResources.MenuPage_LogoutPrompt_Text, AppResources.MenuPage_LogoutPrompt_Confirm, AppResources.MenuPage_LogoutPrompt_Cancel);

            if (result) {
                Logger.Info("User requested logout.");

                _ = client.logoutUser();

                DataService.setConfigValue(DataService.DATA_KEYS.USER_NAME, AppResources.MenuPage_UserName_Default);
                DataService.setConfigValue(DataService.DATA_KEYS.USER_PHONE, AppResources.MenuPage_UserPhone_Default);
                DataService.setConfigValue(DataService.DATA_KEYS.USER_HAS_PHOTO, false);
                
                DataService.deleteAllAlertConfigs();
                MessagingCenter.Send(this, MESSAGING_KEYS.LOGOUT_USER.ToString());

                IsPresented = false;
                
            }
        }

        private async Task requestAboutPageNavigation() {
            await Detail.Navigation.PushAsync(new AboutPage());
            IsPresented = false;
        }

        private void requestNotificationSettings() {
            Interfaces.INavigation navigationInterface = DependencyService.Get<Interfaces.INavigation>();
            navigationInterface.navigateNotificationSettings();
            IsPresented = false;
        }

        private async Task requestShare() {
            //https://docs.microsoft.com/en-us/xamarin/essentials/share?context=xamarin%2Fios&tabs=android
            ShareTextRequest request = new ShareTextRequest(AppResources.App_Share_Message) {
                Uri = "http://www.bartunik.de/pagerbuddy"
            };
            await Share.RequestAsync(request);
            IsPresented = false;
        }

        private async Task requestWebsite() {
            await Launcher.OpenAsync("https://www.facebook.com/pagerbuddy");
            IsPresented = false;
        }
    }
}