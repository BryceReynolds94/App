using PagerBuddy.Resources;
using System;
using System.Collections.Generic;
using System.Text;

namespace PagerBuddy.Services {
    public class UpdaterService {

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void checkUpdate(string previousBuild, string currentBuild) {

            bool parsePrevOK = int.TryParse(previousBuild, out int previousVersion);
            bool parseCurrOK = int.TryParse(currentBuild, out int currentVersion);

            if(!parsePrevOK || !parseCurrOK) {
                Logger.Warn("Could not parse build strings to check for update changes.");
                return;
            }

            int updateBuildStatus = DataService.getConfigValue(DataService.DATA_KEYS.BUILD_UPDATE_COMPLETE, 0);

            DataService.setConfigValue(DataService.DATA_KEYS.BUILD_UPDATE_COMPLETE, currentVersion);
        }

        public static bool checkNotification(string previousBuild, string currentBuild) {

            bool parsePrevOK = int.TryParse(previousBuild, out int previousVersion);
            bool parseCurrOK = int.TryParse(currentBuild, out int currentVersion);

            if (!parsePrevOK || !parseCurrOK) {
                Logger.Warn("Could not parse build strings to check for update notification.");
                return false;
            }

            if (false) { //Would set Update check here
                return true;
            }
            return false;
        }

    }
}
