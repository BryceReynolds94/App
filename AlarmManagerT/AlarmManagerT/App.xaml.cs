using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using PagerBuddy.Services;
using PagerBuddy.Views;
using System.Collections;
using PagerBuddy.ViewModels;
using Plugin.FirebasePushNotification;
using PagerBuddy.Interfaces;
using System.Threading.Tasks;
using PagerBuddy.Models;
using Xamarin.Essentials;
using System.Collections.ObjectModel;
using PagerBuddy.Resources;

namespace PagerBuddy
{

    //TODO Later: Implement rating prompt https://developer.android.com/guide/playcore/in-app-review/ (similar for iOS)
    public partial class App : Application
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public App(bool isAlert, Alert alert)
        {
            initialise(isAlert, alert);
        }

        public App()
        {
            initialise(false);
        }

        private void initialise(bool isAlert, Alert alert = null)
        {
            InitializeComponent();
            VersionTracking.Track(); //Initialise global version tracking

            MessagingCenter.Subscribe<AboutPage>(this, AboutPage.MESSAGING_KEYS.SHOW_ALERT_PAGE.ToString(), (sender) => showAlertPage());

            if (isAlert)
            {
                MainPage = new AlertPage(alert);
                return;
            }

            MainPage = new MainPage();
            new MessagingService().SetupListeners(((MainPage)Current.MainPage).client);
        }

        private void showAlertPage() {
            Collection<string> configs = DataService.getConfigList();
            Alert testAlert;
            if(configs.Count > 0) {
                AlertConfig config = DataService.getAlertConfig(configs[0]);
                testAlert = new Alert(AppResources.App_DeveloperMode_AlertPage_Message, config);
            } else {
                Logger.Info("No configurations found. Using mock configuration for sample AlertPage.");
                testAlert = new Alert(AppResources.App_DeveloperMode_AlertPage_Title, AppResources.App_DeveloperMode_AlertPage_Message, "", 0, false);
            }
            Logger.Info("Launchin AlertPage from Developer Mode");
            MainPage = new AlertPage(testAlert);
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
