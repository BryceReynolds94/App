using PagerBuddy.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Xamarin.Forms;

namespace PagerBuddy.Services
{
    public class DataService
    {

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static string saveLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public enum DATA_KEYS
        { //Changes will break Updates!
            ALERT_CONFIG_LIST, //List of all configurations
            CONFIG_DEACTIVATE_ALL, //All notifications disabled
            CONFIG_SNOOZE_ALL, //All notifications disabled temporarily
            USER_NAME, //Name of Telegram user for display
            USER_PHONE, //Phone number of Telegram user for display
            USER_HAS_PHOTO, //Whether user has a profile pic
            USER_PHOTO,
            DEVELOPER_MODE,
            REFRESH_LOCK_TIME, //Time untill incoming FCM messages should be ignored
            LAST_REFRESH_TIME, //Last time new messages were collected from Telegram
            HAS_PROMPTED_DND_PERMISSION, //Whether the user has been asked to grant DND Permission in Android
            FCM_TOKEN, //Token for FCM messages
            LAST_MESSAGE_ID
        }; 


        public static AlertConfig getAlertConfig(string id)
        {
            string confString = read(id, (string) null);
            if(confString == null) {
                confString = serialiseObject(new AlertConfig());
            }
            return deserialiseObject<AlertConfig>(confString);
        }

        public static void saveAlertConfig(AlertConfig alertConfig)
        {
            Collection<string> configList = getConfigList();
            if (!configList.Contains(alertConfig.id))
            {
                configList.Add(alertConfig.id);
                save(DATA_KEYS.ALERT_CONFIG_LIST.ToString(), serialiseObject(configList));
            }
            updateAlertConfig(alertConfig);
        }

        public static void deleteAlertConfig(AlertConfig alertConfig)
        {
            Collection<string> configList = getConfigList();
            configList.Remove(alertConfig.id);
            removeProfilePic(alertConfig.id);

            save(alertConfig.id, (string) null);
            save(DATA_KEYS.ALERT_CONFIG_LIST.ToString(), serialiseObject(configList));
            persist();
        }

        public static void updateAlertConfig(AlertConfig alertConfig)
        {
            save(alertConfig.id, serialiseObject(alertConfig));
            persist();
        }

        public static void saveProfilePic(string name, MemoryStream image)
        {
            File.WriteAllBytes(profilePicSavePath(name), image.ToArray());
        }

        public static void removeProfilePic(string name)
        {
            if (File.Exists(profilePicSavePath(name))){
                File.Delete(profilePicSavePath(name));
            }
        }

        public static string profilePicSavePath(string name)
        {
            return Path.Combine(saveLocation, name + ".profilePic");
        }

        public static Collection<string> getConfigList()
        {
            string list = read(DATA_KEYS.ALERT_CONFIG_LIST.ToString(), (string) null);

            if(list == null) {
                list = serialiseObject(new Collection<string>());
                save(DATA_KEYS.ALERT_CONFIG_LIST.ToString(), list);
            }
            return deserialiseObject<Collection<string>>(list);
        }

        public static T getConfigValue<T>(DATA_KEYS key, T defaultValue)
        {
            return read(key.ToString(), defaultValue);
        }

        public static void setConfigValue(DATA_KEYS key, object value)
        {
            if(value == null || !(value.GetType().IsPrimitive || value is string))
            {
                Logger.Error("Trying to save an invalid data type to DATA_KEY " + key.ToString());
                return;
            }
            save(key.ToString(), value);
        }

        private static void save<T>(string key, T value) {
            Application.Current.Properties[key] = value;
        }

        private static T read<T>(string key, T defaultValue) {
            T value;
            try {
                value = (T) Application.Current.Properties[key];
            } catch (Exception e) {
                Logger.Debug(e, "Could not find DATA_KEY " + key);
                return defaultValue;
            }
            return value;
        }

        private static void persist() {
            Application.Current.SavePropertiesAsync();
        }

        public static string serialiseObject(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static T deserialiseObject<T>(string str)
        {
            return JsonConvert.DeserializeObject<T>(str);
        }

    }
}
