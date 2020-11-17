using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using AlarmManagerT.Models;
using AlarmManagerT.Services;
using TeleSharp.TL;
using AlarmManagerT.Resources;

namespace AlarmManagerT.Views {
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : MasterDetailPage {

        private NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public CommunicationService client;

        public enum MESSAGING_KEYS { LOGOUT_USER};
        public MainPage() {
            InitializeComponent();

            MasterBehavior = MasterBehavior.Popover;
            client = new CommunicationService();

            Detail = new NavigationPage(new HomeStatusPage(client));

            MessagingCenter.Subscribe<AboutPage>(this, AboutPage.MESSAGING_KEYS.RESTART_CLIENT.ToString(), async (sender) => await client.forceReloadConnection());
        }

        public async Task NavigateFromMenu(MenuPage.MENU_PAGE destination) {
            switch (destination) {
                case MenuPage.MENU_PAGE.NotificationSettings:
                    requestNotificationSettings();
                    break;
                case MenuPage.MENU_PAGE.Share:
                    requestShare();
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
                default:
                    break;
            }
        }

        private async Task LoginUser() {
            if(client.clientStatus == CommunicationService.STATUS.OFFLINE || client.clientStatus == CommunicationService.STATUS.AUTHORISED) {
                return;
            }
            await Detail.Navigation.PushAsync(new LoginPhonePage(client));
            IsPresented = false;
        }

        private async Task LogoutUser() {
            if(client.clientStatus != CommunicationService.STATUS.AUTHORISED) {
                return;
            }
            bool result = await DisplayAlert(AppResources.MenuPage_LogoutPrompt_Title, AppResources.MenuPage_LogoutPrompt_Text, AppResources.MenuPage_LogoutPrompt_Confirm, AppResources.MenuPage_LogoutPrompt_Cancel);

            if (result) {
                Logger.Info("User requested logout.");

                await client.logoutUser();

                DataService.setConfigValue(DataService.DATA_KEYS.USER_NAME, AppResources.MenuPage_UserName_Default);
                DataService.setConfigValue(DataService.DATA_KEYS.USER_PHONE, AppResources.MenuPage_UserPhone_Default);
                DataService.setConfigValue(DataService.DATA_KEYS.USER_HAS_PHOTO, false);

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

        private void requestShare() {
            Interfaces.INavigation navigationInterface = DependencyService.Get<Interfaces.INavigation>();
            navigationInterface.navigateShare("SAMPLE MESSAGE"); //TODO: Write Message to share
            IsPresented = false;
        }
    }
}