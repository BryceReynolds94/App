using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using PagerBuddy.Services;
using PagerBuddy.Views;
using System.Collections;
using PagerBuddy.ViewModels;
using PagerBuddy.Interfaces;
using System.Threading.Tasks;
using PagerBuddy.Models;
using Xamarin.Essentials;
using System.Collections.ObjectModel;
using PagerBuddy.Resources;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace PagerBuddy
{
    //MEMO: NLog Setup: https://www.jenx.si/2020/07/15/using-nlog-in-xamarin-forms-applications/
    //MEMO: FFImageLoading Setup: https://github.com/luberda-molinet/FFImageLoading/wiki/Xamarin.Forms-API
    //MEMO: FirebasePushNotificationPlugin Setup: https://github.com/CrossGeeks/FirebasePushNotificationPlugin/blob/master/docs/GettingStarted.md
    //MEMO: Generate app icon assets: https://easyappicon.com/ https://www.iconsgenerator.com/


    //TODO Later: Implement rating prompt https://developer.android.com/guide/playcore/in-app-review/ (similar for iOS)

    public partial class App : Application
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public App(bool isAlert = false, Alert alert = null)
        {
            Logger.Debug("Starting App.");
            InitializeComponent();
            VersionTracking.Track(); //Initialise global version tracking

            if (VersionTracking.IsFirstLaunchEver) {
                Logger.Debug("Fresh installation. First run of App with build No. " + VersionTracking.CurrentBuild);
            }else if (VersionTracking.IsFirstLaunchForCurrentBuild) {
                Logger.Debug("App updated from build no. " + VersionTracking.PreviousBuild + ". First run of build no. " + VersionTracking.CurrentBuild);
                UpdaterService.checkUpdate(VersionTracking.PreviousBuild, VersionTracking.CurrentBuild);
            }

            if (isAlert && alert != null) {
                MainPage = new AlertPage(alert);
                return;
            }

            MessagingCenter.Subscribe<AboutPage>(this, AboutPage.MESSAGING_KEYS.SHOW_ALERT_PAGE.ToString(), (sender) => showAlertPage());
            MainPage = new MainPage();
            new MessagingService(((MainPage)Current.MainPage).client);
        }

        private void showAlertPage() {
            Collection<string> configs = DataService.getConfigList();
            Alert testAlert;
            if(configs.Count > 0) {
                AlertConfig config = DataService.getAlertConfig(configs[0], null);
                if(config == null) {
                    Logger.Error("Loading a known alert config returned null. Stopping here.");
                    return;
                }
                testAlert = new Alert(AppResources.App_DeveloperMode_AlertPage_Message, config);
            } else {
                Logger.Info("No configurations found. Using mock configuration for sample AlertPage.");
                testAlert = new Alert(AppResources.App_DeveloperMode_AlertPage_Title, AppResources.App_DeveloperMode_AlertPage_Message, "", 0, false, TelegramPeer.TYPE.CHAT);
            }
            Logger.Info("Launching AlertPage from Developer Mode");
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
