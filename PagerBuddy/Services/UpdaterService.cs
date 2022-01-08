using PagerBuddy.Resources;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace PagerBuddy.Services {
    public class UpdaterService {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void checkUpdate(string previousBuild, string currentBuild) {

            bool parsePrevOK = int.TryParse(previousBuild, out int previousVersion);
            bool parseCurrOK = int.TryParse(currentBuild, out int currentVersion);

            if(!parsePrevOK || !parseCurrOK) {
                Logger.Warn("Could not parse build strings to check for update changes.");
                return;
            }

            int updateBuildStatus = DataService.getConfigValue(DataService.DATA_KEYS.BUILD_UPDATE_COMPLETE, 0);

            if (updateBuildStatus != currentVersion && currentVersion >= 35 && previousVersion < 35 && Device.RuntimePlatform == Device.Android) { //Would set Update check here
                //Update to V2.0 (breaking some data structures)
                Logger.Info("Detected update across v2 threshold. Deleting user preferences to ensure long-term compatibility. Keeping client session.");
                bool promptedWelcome = DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_WELCOME, false);
                bool promptedDND = DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DND_PERMISSION, false);
                DataService.clearData(false, promptedWelcome);
                DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DND_PERMISSION, promptedDND);
            }

            DataService.setConfigValue(DataService.DATA_KEYS.BUILD_UPDATE_COMPLETE, currentVersion);
        }

        public static bool checkNotification(string previousBuild, string currentBuild) {

            bool parsePrevOK = int.TryParse(previousBuild, out int previousVersion);
            bool parseCurrOK = int.TryParse(currentBuild, out int currentVersion);

            if (!parsePrevOK || !parseCurrOK) {
                Logger.Warn("Could not parse build strings to check for update notification.");
                return false;
            }

            if (currentVersion >= 35 && previousVersion < 35) { //Would set Update check here
                //Update to V2.0 (possibly breaking everything)
                return true;
            }
            return false;
        }

    }
}
