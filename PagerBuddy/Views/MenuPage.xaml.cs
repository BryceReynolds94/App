using PagerBuddy.Models;
using PagerBuddy.Resources;
using PagerBuddy.Services;
using PagerBuddy.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PagerBuddy.Views {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MenuPage : ContentPage {

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        MainPage RootPage { get => Application.Current.MainPage as MainPage; }

        public enum MENU_PAGE { AboutPage, LogoutUser, NotificationSettings, Share, Login, Website, Close };

        private readonly MenuPageViewModel viewModel;
        public MenuPage() {
            InitializeComponent();

            BindingContext = viewModel = new MenuPageViewModel();
            viewModel.RequestNavigation += requestNavigation;
            viewModel.RequestTestAlert += testAlert;

            MessagingCenter.Subscribe<HomeStatusPage>(this, HomeStatusPage.MESSAGING_KEYS.ALERT_CONFIGS_CHANGED.ToString(), (_) => viewModel.configsChanged());
            MessagingCenter.Subscribe<MainPage>(this, MainPage.MESSAGING_KEYS.LOGOUT_USER.ToString(), (_) => viewModel.configsChanged());
        }

        private async void requestNavigation(object sender, MENU_PAGE destination) {
            await RootPage.NavigateFromMenu(destination);
        }

        private void testAlert(object sender, EventArgs args) {
            Logger.Info("User requested test alert.");

            Collection<string> configs = DataService.getConfigList();
            if (configs.Count > 0) {

                AlertConfig config = DataService.getAlertConfig(configs.First(), null);
                if (config == null) {
                    Logger.Error("Retrieving known alert returned null. Will stop here.");
                    return;
                }

                Interfaces.INotifications notifications = DependencyService.Get<Interfaces.INotifications>();
                notifications.showToast(AppResources.MenuPager_TestNotification_Warning);
                if (Device.RuntimePlatform == Device.Android) {
                    Task.Delay(5000).ContinueWith((t) => {
                        Logger.Info("Sending notification test message now.");
                        notifications.showAlertNotification(new Alert(AppResources.MenuPage_TestNotification_Message, DateTime.Now, false, config));
                    });
                } else {
                    Logger.Info("Scheduling notification test message.");
                    notifications.showAlertNotification(new Alert(AppResources.MenuPage_TestNotification_Message, DateTime.Now, false, config));
                }
                requestNavigation(this, MENU_PAGE.Close);
            } else {
                Logger.Warn("Could not send notification test message as no alerts are configured.");
            }
        }

    }
}