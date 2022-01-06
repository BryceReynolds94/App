using PagerBuddy.Models;
using PagerBuddy.Resources;
using PagerBuddy.Services;
using PagerBuddy.ViewModels;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;

namespace PagerBuddy.Views {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AboutPage : ContentPage {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly AboutPageViewModel viewModel;

        public enum MESSAGING_KEYS { SHOW_ALERT_PAGE, RESTART_CLIENT }
        public AboutPage() {
            InitializeComponent();

            BindingContext = viewModel = new AboutPageViewModel();
            viewModel.requestRestartClient += restartClient;
            viewModel.requestShareLog += shareLog;
            viewModel.requestShowAlertPage += showAlertPage;

            //TODO: Make test alert available to public
            viewModel.requestTestFCMMessage += testFCMMessage;
            viewModel.requestTestNotification += testNotification;
            viewModel.requestClearData += clearData;
            viewModel.requestCheckPermissions += checkPermissions;
        }

        public static string getLogFileLocation() {
            FileTarget target = NLog.LogManager.Configuration.FindTargetByName<FileTarget>("logfile");
            if (target == null) {
                Logger.Error("Could not finde log target.");
                return null;
            }
            string logFile = target.FileName.Render(new NLog.LogEventInfo { TimeStamp = DateTime.Now });

            if (!File.Exists(logFile)) {
                Logger.Error("Log file could not be found.");
                return null;
            }
            return logFile;
        }

        private async void shareLog(object sender, EventArgs args) {
            Logger.Info("Launched Log sharing");

            Interfaces.IPermissions navigation = DependencyService.Get<Interfaces.IPermissions>();
            navigation.logPermissionSettings();

            Logger.Debug("App build version: " + VersionTracking.CurrentBuild);
            Logger.Debug("Device: " + DeviceInfo.Manufacturer + ", " + DeviceInfo.Model);
            Logger.Debug("OS: " + DeviceInfo.Platform + " " + DeviceInfo.VersionString);

            string logFile = getLogFileLocation();
            if (logFile != null) {
                await Share.RequestAsync(new ShareFileRequest(new ShareFile(logFile)));
            } else {
                Logger.Warn("Could not share log file as no file was found.");
            }
        }

        private void clearData(object sender, EventArgs args) {
            DataService.clearData(true);
            MySessionStore.Clear();

            Logger.Info("Cleared all set preferences and the client session file.");
        }

        private async void checkPermissions(object sender, EventArgs args) {
            Logger.Info("User requested permissions check.");

            Interfaces.IPermissions permissions = DependencyService.Get<Interfaces.IPermissions>();
            await permissions.checkAlertPermissions(this, true);
        }

        private void showAlertPage(object sender, EventArgs args) {
            Logger.Info("User requested view AlertPage.");

            Collection<string> configs = DataService.getConfigList();
            Alert testAlert;
            if (configs.Count > 0) {
                AlertConfig config = DataService.getAlertConfig(configs[0], null);
                if (config == null) {
                    Logger.Error("Loading a known alert config returned null. Stopping here.");
                    return;
                }
                testAlert = new Alert(AppResources.App_DeveloperMode_AlertPage_Message, DateTime.Now, false, config);
            } else {
                Logger.Info("No configurations found. Using mock configuration for sample AlertPage.");
                testAlert = new Alert(AppResources.App_DeveloperMode_AlertPage_Title, AppResources.App_DeveloperMode_AlertPage_Message, "", 0, false, DateTime.Now, false, TelegramPeer.TYPE.CHAT);
            }
            Logger.Info("Launching AlertPage from Developer Mode");

            MessagingCenter.Send(this, MESSAGING_KEYS.SHOW_ALERT_PAGE.ToString(), testAlert);
        }

        private void restartClient(object sender, EventArgs args) {
            Logger.Info("Restarting client on user request.");
            MessagingCenter.Send(this, MESSAGING_KEYS.RESTART_CLIENT.ToString());
        }

        private void testFCMMessage(object sender, EventArgs args) {
            Logger.Info("Launching FCM Message Handling as if an external update was received.");

            Collection<string> configs = DataService.getConfigList();
            if (configs.Count > 0) {
                Logger.Info("Sending alert test message");
                AlertConfig config = DataService.getAlertConfig(configs.First(), null);
                if(config == null) {
                    Logger.Error("Retrieving known alert returned null. Will stop here.");
                    return;
                }
                AlertService.checkMessage(AppResources.AboutPage_DeveloperMode_TestNotification_Message, config.triggerGroup.id, DateTime.Now, false);
            } else {
                Logger.Warn("Could not send alert test message as no alerts are configured.");
            }
        }

        private void testNotification(object sender, EventArgs args) {
            Collection<string> configs = DataService.getConfigList();
            if (Device.RuntimePlatform == Device.Android) {
                if (configs.Count > 0) {
                    Logger.Info("Sending notification test message in 5s.");
                    AlertConfig config = DataService.getAlertConfig(configs.First(), null);
                    if (config == null) {
                        Logger.Error("Retrieving known alert returned null. Will stop here.");
                        return;
                    }
                    Interfaces.IAndroidNotification notifications = DependencyService.Get<Interfaces.IAndroidNotification>();

                    Task.Delay(5000).ContinueWith((t) => {
                        Logger.Info("Sending notification test message now.");
                        notifications.showAlertNotification(new Alert(AppResources.AboutPage_DeveloperMode_TestNotification_Message, DateTime.Now, false, config));
                    });
                } else {
                    Logger.Warn("Could not send notification test message as no alerts are configured.");
                }
            } else {
                Logger.Warn("Test alert is not supported on this device platform.");
            }
        }
    }
}