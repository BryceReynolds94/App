using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using PagerBuddy.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.Droid.SystemLogger))] //register for dependency service as platform-specific code
namespace PagerBuddy.Droid {
    class SystemLogger : IAndroidSystemLogger {

        private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public string getSystemLogFile() {

            try {
                Java.Lang.Process process = Runtime.GetRuntime().Exec("logcat -v long -d");

                string logFileLocation = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "pagerbuddylogcat.txt");
                using (FileStream fileStream = new FileStream(logFileLocation, FileMode.Create, FileAccess.Write)) {
                    process.InputStream.CopyTo(fileStream);
                }

                return logFileLocation;

            } catch (IOException e) {
                Logger.Error(e, "Exception trying to write logcat to file.");
            }
            return null;
        }

    }
}