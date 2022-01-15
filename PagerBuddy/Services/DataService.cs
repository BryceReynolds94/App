using PagerBuddy.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Xamarin.Forms;
using Xamarin.Essentials;
using System.Threading.Tasks;

namespace PagerBuddy.Services {
    public class DataService {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly string saveLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public enum DATA_KEYS { //Changes will break Updates!
            ALERT_CONFIG_LIST, //List of all configurations
            CONFIG_DEACTIVATE_ALL, //All notifications disabled
            CONFIG_SNOOZE_ALL, //All notifications disabled temporarily
            USER_NAME, //Name of Telegram user for display
            USER_PHONE, //Phone number of Telegram user for display
            USER_HAS_PHOTO, //Whether user has a profile pic
            USER_PHOTO,
            DEVELOPER_MODE,
            DEVELOPER_LOG_LEVEL, //LogLevel set vor DeveloperView (AboutPage)
            HAS_PROMPTED_DND_PERMISSION, //Whether the user has been asked to grant DND Permission in Android
            HAS_PROMPTED_HIBERNATION_EXCLUSION, //Whether the user has been asked to exempt the app from Android hibernation restrictions
            HAS_PROMPTED_WELCOME, //Whether the first-use welcome screen has been shown
            HAS_PROMPTED_IOS_NOTIFICATION_PERMISSION,
            FCM_TOKEN, //Token for FCM/APNS messages
            BUILD_UPDATE_COMPLETE, //Build ID to avoid performing double updates (mostly Samsung)
            ACTIVE_TIME_DAYS, //Days of the week where alerts are active 
            ACTIVE_TIME_FROM, //Time of day from when alerts are active
            ACTIVE_TIME_TO, //Time of day untill when alerts are active
            ACTIVE_TIME_INVERT, //If the set times are when alerts are inactive
            CUSTOM_PAGERBUDDY_SERVER_BOT_LIST, //List of user set peers that should be accepted as pagerbuddy servers
        };

        public static void clearData(bool developerMode = true, bool hasPromptedWelcome = true) //Caution! Use with care
        {
            Preferences.Clear();
            setConfigValue(DATA_KEYS.DEVELOPER_MODE, developerMode);
            setConfigValue(DATA_KEYS.HAS_PROMPTED_WELCOME, hasPromptedWelcome);
            setConfigValue(DATA_KEYS.BUILD_UPDATE_COMPLETE, VersionTracking.CurrentBuild);
        }


        public static AlertConfig getAlertConfig(string id, AlertConfig defaultValue) {
            if (!Preferences.ContainsKey(id)) {
                Logger.Debug("Could not find AlertConfig with ID " + id);
                deleteAlertConfig(id);
                return defaultValue;
            } else {
                string confString = Preferences.Get(id, null);
                if (confString == null) {
                    deleteAlertConfig(id);
                    return defaultValue;
                }
                return deserialiseObject<AlertConfig>(confString);
            }

        }

        public static void saveAlertConfig(AlertConfig alertConfig) {
            Collection<string> configList = getConfigList();
            if (!configList.Contains(alertConfig.id)) {
                configList.Add(alertConfig.id);
                setConfigValue(DATA_KEYS.ALERT_CONFIG_LIST, serialiseObject(configList));
            }
            updateAlertConfig(alertConfig);
        }

        public static void deleteAlertConfig(AlertConfig alertConfig) {
            deleteAlertConfig(alertConfig.id);
        }

        public static void deleteAlertConfig(string id) {
            Collection<string> configList = getConfigList();
            configList.Remove(id);
            removeProfilePic(id);

            if (Preferences.ContainsKey(id)) {
                Preferences.Remove(id);
            }

            setConfigValue(DATA_KEYS.ALERT_CONFIG_LIST, serialiseObject(configList));
        }

        public static void deleteAllAlertConfigs() {
            Collection<string> configList = getConfigList();
            foreach (string id in configList) {
                if (Preferences.ContainsKey(id)) {
                    Preferences.Remove(id);
                }
            }
            configList.Clear();
            setConfigValue(DATA_KEYS.ALERT_CONFIG_LIST, serialiseObject(configList));
        }


        public static void updateAlertConfig(AlertConfig alertConfig) {
            Preferences.Set(alertConfig.id, serialiseObject(alertConfig));
        }

        public static void saveProfilePic(string name, MemoryStream image) {
            File.WriteAllBytes(profilePicSavePath(name), image.ToArray());
        }

        public static void removeProfilePic(string name) {
            if (File.Exists(profilePicSavePath(name))) {
                File.Delete(profilePicSavePath(name));
            }
        }

        public static string profilePicSavePath(string name) {
            return Path.Combine(saveLocation, name + ".profilePic");
        }

        public static Collection<string> getConfigList() {
            string list = getConfigValue(DATA_KEYS.ALERT_CONFIG_LIST, serialiseObject(new Collection<string>()));
            return deserialiseObject<Collection<string>>(list);
        }
        public static Collection<DayOfWeek> activeDays {
            get {
                Collection<DayOfWeek> defaultCollection = new Collection<DayOfWeek>() {
                        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
                    };
                string list = getConfigValue(DATA_KEYS.ACTIVE_TIME_DAYS, serialiseObject(defaultCollection));
                return deserialiseObject<Collection<DayOfWeek>>(list);
            }
            set {
                setConfigValue(DATA_KEYS.ACTIVE_TIME_DAYS, serialiseObject(value));
            }
        }

        public static List<string> customPagerBuddyServerBots {
            get {
                string list = getConfigValue(DATA_KEYS.CUSTOM_PAGERBUDDY_SERVER_BOT_LIST, serialiseObject(new List<string>()));
                return deserialiseObject<List<string>>(list);
            }
            set {
                setConfigValue(DATA_KEYS.CUSTOM_PAGERBUDDY_SERVER_BOT_LIST, serialiseObject(value));
            }
        }

        public static string getConfigValue(DATA_KEYS key, string defaultValue) {
            if (!Preferences.ContainsKey(key.ToString())) {
                Logger.Debug("Could not find DATA_KEY " + key + ". Setting default value.");
                setConfigValue(key, defaultValue);
                return defaultValue;
            }

            string value;
            try {
                value = Preferences.Get(key.ToString(), defaultValue);
            } catch (Exception e) {
                Logger.Error(e, "Error trying to retrieve DATA_KEY " + key + ". This may be due to type mismatch. Setting default value.");
                setConfigValue(key, defaultValue);
                return defaultValue;
            }
            return value;
        }
        public static bool getConfigValue(DATA_KEYS key, bool defaultValue) {
            if (!Preferences.ContainsKey(key.ToString())) {
                Logger.Debug("Could not find DATA_KEY " + key + ". Setting default value.");
                setConfigValue(key, defaultValue);
                return defaultValue;
            }

            bool value;
            try {
                value = Preferences.Get(key.ToString(), defaultValue);
            } catch (Exception e) {
                Logger.Error(e, "Error trying to retrieve DATA_KEY " + key + ". This may be due to type mismatch. Setting default value.");
                setConfigValue(key, defaultValue);
                return defaultValue;
            }
            return value;
        }
        public static int getConfigValue(DATA_KEYS key, int defaultValue) {
            if (!Preferences.ContainsKey(key.ToString())) {
                Logger.Debug("Could not find DATA_KEY " + key + ". Setting default value.");
                setConfigValue(key, defaultValue);
                return defaultValue;
            }

            int value;
            try {
                value = Preferences.Get(key.ToString(), defaultValue);
            } catch (Exception e) {
                Logger.Error(e, "Error trying to retrieve DATA_KEY " + key + ". This may be due to type mismatch. Setting default value.");
                setConfigValue(key, defaultValue);
                return defaultValue;
            }
            return value;
        }
        public static long getConfigValue(DATA_KEYS key, long defaultValue) {
            if (!Preferences.ContainsKey(key.ToString())) {
                Logger.Debug("Could not find DATA_KEY " + key + ". Setting default value.");
                setConfigValue(key, defaultValue);
                return defaultValue;
            }

            long value;
            try {
                value = Preferences.Get(key.ToString(), defaultValue);
            } catch (Exception e) {
                Logger.Error(e, "Error trying to retrieve DATA_KEY " + key + ". This may be due to type mismatch. Setting default value.");
                setConfigValue(key, defaultValue);
                return defaultValue;
            }
            return value;
        }
        public static DateTime getConfigValue(DATA_KEYS key, DateTime defaultValue) {
            if (!Preferences.ContainsKey(key.ToString())) {
                Logger.Debug("Could not find DATA_KEY " + key + ". Setting default value.");
                setConfigValue(key, defaultValue);
                return defaultValue;
            }

            DateTime value;
            try {
                value = Preferences.Get(key.ToString(), defaultValue);
            } catch (Exception e) {
                Logger.Error(e, "Error trying to retrieve DATA_KEY " + key + ". This may be due to type mismatch. Setting default value.");
                setConfigValue(key, defaultValue);
                return defaultValue;
            }
            return value;
        }
        public static byte[] getConfigValue(DATA_KEYS key, byte[] defaultValue) {
            return deserialiseObject<byte[]>(getConfigValue(key, serialiseObject(defaultValue)));
        }

        public static void setConfigValue(DATA_KEYS key, bool value) {
            Preferences.Set(key.ToString(), value);
        }
        public static void setConfigValue(DATA_KEYS key, string value) {
            Preferences.Set(key.ToString(), value);
        }
        public static void setConfigValue(DATA_KEYS key, int value) {
            Preferences.Set(key.ToString(), value);
        }
        public static void setConfigValue(DATA_KEYS key, long value) {
            Preferences.Set(key.ToString(), value);
        }
        public static void setConfigValue(DATA_KEYS key, DateTime value) {
            Preferences.Set(key.ToString(), value);
        }

        public static void setConfigValue(DATA_KEYS key, byte[] value) {
            Preferences.Set(key.ToString(), serialiseObject(value));
        }

        public static string serialiseObject(object obj) {
            return JsonConvert.SerializeObject(obj);
        }

        public static T deserialiseObject<T>(string str) {
            return JsonConvert.DeserializeObject<T>(str);
        }

    }
}
