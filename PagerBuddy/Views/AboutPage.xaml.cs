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
            viewModel.requestShareSystemLog += shareSystemLog;
            viewModel.requestShowAlertPage += showAlertPage;
            viewModel.requestTestFCMMessage += testFCMMessage;
            viewModel.requestClearData += clearData;
            viewModel.requestCheckPermissions += checkPermissions;
        }

        public static string getLogFileLocation() {
            FileTarget target = NLog.LogManager.Configuration.FindTargetByName<FileTarget>("logfile");
            if (target == null) {
                Logger.Error("Could not find log target.");
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

        private async void shareSystemLog(object sender, EventArgs args) {
            if(Device.RuntimePlatform == Device.Android) {
                Logger.Info("Sharing Logcat");

                Interfaces.IAndroidSystemLogger systemLogger = DependencyService.Get<Interfaces.IAndroidSystemLogger>();

                string logFile = systemLogger.getSystemLogFile();
                if (logFile != null) {
                    await Share.RequestAsync(new ShareFileRequest(new ShareFile(logFile)));
                } else {
                    Logger.Warn("Could not share logcat file as no file was found.");
                }

            } else {
                Logger.Warn("System log sharing not implemented for this device platform.");
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
            if (Device.RuntimePlatform == Device.Android) {
                Logger.Info("Launching FCM Message Handling as if an external update was received.");

                Collection<string> configs = DataService.getConfigList();
                if (configs.Count > 0) {
                    Logger.Info("Sending alert test message");
                    AlertConfig config = DataService.getAlertConfig(configs.First(), null);
                    if (config == null) {
                        Logger.Error("Retrieving known alert returned null. Will stop here.");
                        return;
                    }
                    simulateFCMPayload(config);
                    //AlertService.checkMessage(AppResources.AboutPage_DeveloperMode_TestNotification_Message, config.triggerGroup.serverID, DateTime.Now, false, false);
                } else {
                    Logger.Warn("Could not send alert test message as no alerts are configured.");
                }
            } else {
                Logger.Warn("Test FCM is not supported on this device platform.");
            }
        }

        private void simulateFCMPayload(AlertConfig config) {
            //This should only be called on Android and only for internal debugging

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("zvei", "99999");
            data.Add("is_test_alert", "false");
            data.Add("zvei_description", AppResources.AboutPage_DeveloperMode_TestNotification_Message);
            data.Add("is_manual_test_alert", "false");
            data.Add("chat_id", config.triggerGroup.serverID.ToString());
            //data.Add("chat_id", "-1001375064719.0"); //B1 Group

            TimeSpan alertTimestamp = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            data.Add("alert_timestamp", ((long)alertTimestamp.TotalMilliseconds).ToString());

            MessagingService.FirebaseMessage(data);
        }

    }
}