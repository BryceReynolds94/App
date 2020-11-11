using AlarmManagerT.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Xamarin.Forms;

namespace AlarmManagerT.Services
{
    class Data
    {

        private static string saveLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public enum DATA_KEYS {
            ALERT_CONFIG_LIST,
            CONFIG_DEACTIVATE_ALL,
            CONFIG_SNOOZE_ALL,
            USER_NAME,
            USER_PHONE,
            USER_HAS_PHOTO,
            USER_PHOTO,
            REFRESH_LOCK_TIME,
            LAST_REFRESH_TIME
        }; //Changes will break Updates!


        public static AlertConfig getAlertConfig(string id)
        {
            string confString = App.Current.Properties[id] as string;
            AlertConfig config = deserialiseObject<AlertConfig>(confString);
            return config;
        }

        public static void saveAlertConfig(AlertConfig alertConfig)
        {
            Collection<string> configList = getConfigList();
            if (!configList.Contains(alertConfig.id))
            {
                configList.Add(alertConfig.id);
                Application.Current.Properties[DATA_KEYS.ALERT_CONFIG_LIST.ToString()] = serialiseObject(configList);
            }
            updateAlertConfig(alertConfig);
        }

        public static void deleteAlertConfig(AlertConfig alertConfig)
        {
            Collection<string> configList = getConfigList();
            configList.Remove(alertConfig.id);
            removeProfilePic(alertConfig.id);
            Application.Current.Properties[alertConfig.id] = null;
            Application.Current.Properties[DATA_KEYS.ALERT_CONFIG_LIST.ToString()] = serialiseObject(configList);
            //App.Current.SavePropertiesAsync();
        }

        public static void updateAlertConfig(AlertConfig alertConfig)
        {
            Application.Current.Properties[alertConfig.id] = serialiseObject(alertConfig);
            //App.Current.SavePropertiesAsync();
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
            try
            {
                string list = Application.Current.Properties[DATA_KEYS.ALERT_CONFIG_LIST.ToString()] as string;
                return deserialiseObject<Collection<string>>(list);
            }
            catch
            {
                Application.Current.Properties[DATA_KEYS.ALERT_CONFIG_LIST.ToString()] = serialiseObject(new Collection<string>());
            }
            return new Collection<string>();
            
        }

        public static T getConfigValue<T>(DATA_KEYS key, T defaultValue)
        {
            T value;
            try
            {
                value = (T)Application.Current.Properties[key.ToString()];
            }
            catch(Exception e)
            {
                //TODO: Log Error
                return defaultValue;
            }
            return value;
        }

        public static void setConfigValue(DATA_KEYS key, object value)
        {
            if(!(value.GetType().IsPrimitive || value is string))
            {
                //TODO: Throw error
                return;
            }
            Application.Current.Properties[key.ToString()] = value;
        }

        private static string serialiseObject(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        private static T deserialiseObject<T>(string str)
        {
            return JsonConvert.DeserializeObject<T>(str);
        }

    }
}
