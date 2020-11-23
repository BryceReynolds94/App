using PagerBuddy.Resources;
using PagerBuddy.Services;
using PagerBuddy.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace PagerBuddy.ViewModels {
    public class AboutPageViewModel : BaseViewModel {

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public Command DeveloperMode { get; set; }

        public Command HideDeveloperMode { get; set; }
        public Command ShareLog { get; set; }
        public Command ShowAlertPage { get; set; }
        public Command TestNotification { get; set; }
        public Command RestartClient { get; set; }
        public Command TestFCMMessage { get; set; }
        public Command Hyperlink { get; set; }

        private DateTime developerTapStart = DateTime.MinValue;
        private int developerTapCount = 0;
        public AboutPageViewModel() {

            Title = AppResources.AboutPage_Title;
            DeveloperMode = new Command(() => countDeveloperMode());
            HideDeveloperMode = new Command(() => stopDeveloperMode());
            ShareLog = new Command(() => requestShareLog.Invoke(this, null));
            ShowAlertPage = new Command(() => requestShowAlertPage(this, null));
            TestNotification = new Command(() => requestTestNotification(this, null));
            RestartClient = new Command(() => requestRestartClient(this, null));
            TestFCMMessage = new Command(() => requestTestFCMMessage(this, null));
            Hyperlink = new Command<string>(async (url) => await Launcher.OpenAsync(url));

            reloadLogLoop();
        }

        public EventHandler requestShareLog;
        public EventHandler requestShowAlertPage;
        public EventHandler requestTestNotification;
        public EventHandler requestRestartClient;
        public EventHandler requestTestFCMMessage;

        private void reloadLogLoop() {
            if (IsDeveloperMode) {
                OnPropertyChanged(nameof(LogText));
                Task.Delay(5000).ContinueWith((t) => reloadLogLoop());
            }
        }

        private void countDeveloperMode() {
            if (DateTime.Now.Subtract(developerTapStart).TotalSeconds < 2) {
                developerTapCount++;
            } else {
                developerTapCount = 1;
                developerTapStart = DateTime.Now;
            }

            if (developerTapCount > 4) {
                initiateDeveloperMode();
                developerTapCount = 0;
            }
        }

        private void initiateDeveloperMode() {
            Logger.Info("Developer Mode activated.");
            DataService.setConfigValue(DataService.DATA_KEYS.DEVELOPER_MODE, true);
            OnPropertyChanged(nameof(IsDeveloperMode));
            OnPropertyChanged(nameof(NotDeveloperMode));
            reloadLogLoop();
        }

        private void stopDeveloperMode() {
            Logger.Info("Deactivating Developer Mode.");
            DataService.setConfigValue(DataService.DATA_KEYS.DEVELOPER_MODE, false);
            OnPropertyChanged(nameof(IsDeveloperMode));
            OnPropertyChanged(nameof(NotDeveloperMode));
        }

        public string AppVersion {
            get {
                return String.Format(AppResources.AboutPage_App_VersionInfo, VersionTracking.CurrentVersion, VersionTracking.CurrentBuild);
            }
        }

        public bool IsDeveloperMode {
            get {
                return DataService.getConfigValue<bool>(DataService.DATA_KEYS.DEVELOPER_MODE, false);
            }
        }

        public bool NotDeveloperMode { get => !IsDeveloperMode; }

        public string LogText {
            get {
                string logFile = AboutPage.getLogFileLocation();
                if (logFile == null) {
                    return AppResources.AboutPage_DeveloperMode_Log_Default;
                }
                string[] logArray = File.ReadAllLines(logFile);
                Array.Reverse(logArray);
                return string.Join(Environment.NewLine, logArray);
            }
        }
    }
}
