using Newtonsoft.Json;
using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Xamarin.Forms;

namespace PagerBuddy.Models {
    public class ServerRequest {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        //Caution! JSON Property names are used for serialisaton and must match the server implementation!
        [JsonProperty("token")]
        public string Token;
        [JsonProperty("alert_list")]
        public Collection<int> AlertIDList;

        [JsonIgnore]
        public static string PREFIX = "/subscribe ";

        public ServerRequest(string token, Collection<int> alertIDList) {
            this.Token = token;
            this.AlertIDList = alertIDList;
        }

        public static ServerRequest getServerRequest(Collection<AlertConfig> configList) {

            Collection<int> alertIDList = new Collection<int>();
            foreach(AlertConfig config in configList) {
                if (config.isActive) {
                    alertIDList.Add(config.triggerGroup.id);
                }
            }

            string token = DataService.getConfigValue(DataService.DATA_KEYS.FCM_TOKEN, "");
            if (token == null || token.Length < 1) {
                Logger.Warn("Token invalid. Cannot construct ServerRequest");
                return null;
            }

            return new ServerRequest(token, alertIDList);
        }
    }
}
