using PagerBuddy.Resources;
using PagerBuddy.Services;
using PagerBuddy.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace PagerBuddy.ViewModels {
    public class AboutPageViewModel : BaseViewModel {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public Command DeveloperMode { get; set; }
        public Command HideDeveloperMode { get; set; }
        public Command ShareLog { get; set; }
        public Command ShowAlertPage { get; set; }
        public Command RestartClient { get; set; }
        public Command TestFCMMessage { get; set; }
        public Command Hyperlink { get; set; }
        public Command ChangeLogLevel { get; set; }
        public Command CheckPermissions { get; set; }
        public Command ClearData { get; set; }

        public bool HasAndroidFeatures => Device.RuntimePlatform == Device.Android;

        public Dictionary<string, string> LogoColor {
            get {
                Style style = (Style)Application.Current.Resources["AboutLogo"];

                string mode = Application.Current.RequestedTheme == OSAppTheme.Dark ? "Dark" : "Light";
                Setter themeSetter = style.Setters.First((setter) => setter.TargetName == mode);
                string color = ((Color)themeSetter.Value).ToHex();
                return new Dictionary<string, string>() { { "black", color } };
            }
        }

        private enum LogLevel { DEBUG, INFO, WARN, ERROR }
        private LogLevel logLevel;

        private DateTime developerTapStart = DateTime.MinValue;
        private int developerTapCount = 0;

        public AboutPageViewModel() {

            Title = AppResources.AboutPage_Title;

            try{
                logLevel = Enum.Parse<LogLevel>(DataService.getConfigValue(DataService.DATA_KEYS.DEVELOPER_LOG_LEVEL, LogLevel.WARN.ToString()));
            }catch(Exception e) {
                Logger.Error(e, "Exception while parsing saved log level");
                logLevel = LogLevel.WARN;
            }

            DeveloperMode = new Command(() => countDeveloperMode());
            HideDeveloperMode = new Command(() => stopDeveloperMode());
            ShareLog = new Command(() => requestShareLog.Invoke(this, null));
            ShowAlertPage = new Command(() => requestShowAlertPage(this, null));
            RestartClient = new Command(() => requestRestartClient(this, null));
            TestFCMMessage = new Command(() => requestTestFCMMessage(this, null));
            Hyperlink = new Command<string>(async (url) => await Launcher.OpenAsync(url));
            ChangeLogLevel = new Command(() => rotateLogLevel());
            CheckPermissions = new Command(() => requestCheckPermissions(this, null));
            ClearData = new Command(() => requestClearData(this, null));

            Application.Current.RequestedThemeChanged += (s, a) => OnPropertyChanged(nameof(LogoColor));

            reloadLogLoop();
        }

        public EventHandler requestShareLog;
        public EventHandler requestShowAlertPage;
        public EventHandler requestRestartClient;
        public EventHandler requestTestFCMMessage;
        public EventHandler requestClearData;
        public EventHandler requestCheckPermissions;

        private void reloadLogLoop() {
            if (IsDeveloperMode) {
                OnPropertyChanged(nameof(LogText));
                OnPropertyChanged(nameof(OuterLayout));
                Task.Delay(5000).ContinueWith((t) => reloadLogLoop());
            }
        }

        private void rotateLogLevel() {
            if (logLevel == LogLevel.ERROR) {
                logLevel = LogLevel.DEBUG;
            } else {
                logLevel += 1;
            }
            DataService.setConfigValue(DataService.DATA_KEYS.DEVELOPER_LOG_LEVEL, logLevel.ToString());
            OnPropertyChanged(nameof(LogLevelText));
            OnPropertyChanged(nameof(LogText));
        }

        private void countDeveloperMode() {
            if (DateTime.Now.Subtract(developerTapStart).TotalSeconds < 2) {
                developerTapCount++;
            } else {
                developerTapCount = 1;
                developerTapStart = DateTime.Now;
            }

            if (developerTapCount > 9) {
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


        public string OuterLayout => "OuterLayout"; //empty Binding for forcing layout update
        public string AppVersion => string.Format(AppResources.AboutPage_App_VersionInfo, VersionTracking.CurrentVersion, VersionTracking.CurrentBuild);
        public bool IsDeveloperMode => DataService.getConfigValue(DataService.DATA_KEYS.DEVELOPER_MODE, false);
        public bool NotDeveloperMode => !IsDeveloperMode;
        public string LogLevelText => string.Format(AppResources.AboutPage_LogLevel_Prefix, logLevel.ToString());

        public string LogText {
            get {
                string logFile = AboutPage.getLogFileLocation();
                if (logFile == null) {
                    return AppResources.AboutPage_DeveloperMode_Log_Default;
                }

                string[] inArray = File.ReadAllLines(logFile);
                
                //Shorten if Log is very long 
                if(inArray.Length > 100) {
                    inArray = inArray[(inArray.Length - 100)..];
                }


                string[] logArray = applyLogLevel(inArray);
                Array.Reverse(logArray);
                return string.Join(Environment.NewLine, logArray);
            }
        }

        private string[] applyLogLevel(string[] logArray) {
            List<string> outArray = new List<string>();
            foreach (string logEntry in logArray) {
                if (!checkRemoveLogLine(logEntry)) {
                    outArray.Add(logEntry);
                }
            }
            return outArray.ToArray();
        }

        private bool checkRemoveLogLine(string logline) {
            string[] segments = logline.Split("|");
            if(segments.Length < 4) {
                return false;
            }

            string levelText = segments[1];

            bool remove = false;
            switch (logLevel) {
                case LogLevel.ERROR:
                    remove = remove || Regex.IsMatch(levelText, LogLevel.WARN.ToString(), RegexOptions.IgnoreCase);
                    goto case LogLevel.WARN;
                case LogLevel.WARN:
                    remove = remove || Regex.IsMatch(levelText, LogLevel.INFO.ToString(), RegexOptions.IgnoreCase);
                    goto case LogLevel.INFO;
                case LogLevel.INFO:
                    remove = remove || Regex.IsMatch(levelText, LogLevel.DEBUG.ToString(), RegexOptions.IgnoreCase);
                    goto case LogLevel.DEBUG;
                case LogLevel.DEBUG:
                    break;
            }
            return remove;
        }
    }
}
