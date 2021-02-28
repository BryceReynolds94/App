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

            if (previousVersion < 27 && currentVersion >= 27) {
                ToV27();
            }
        }

        private static void ToV27() {
            //Android v27 (1.1.0) was first release with Telega and possibly changed object serialisation
            //We have to clear Telegram session file (before first client init) and all persisted configs
            //User will have to perform a fresh login and setup, DND permission is still persisted

            Logger.Info("Detected update across V27 threshold. Clearing client session and all associated data.");

            //Delete session file
            MySessionStore.Clear();

            //Delete user display data
            DataService.setConfigValue(DataService.DATA_KEYS.USER_NAME, AppResources.MenuPage_UserName_Default);
            DataService.setConfigValue(DataService.DATA_KEYS.USER_PHONE, AppResources.MenuPage_UserPhone_Default);
            DataService.setConfigValue(DataService.DATA_KEYS.USER_HAS_PHOTO, false);

            //Delete alert configs
            DataService.deleteAllAlertConfigs();

            //Clear welcome flag to reprompt login
            DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_WELCOME, false);

        }

    }
}
