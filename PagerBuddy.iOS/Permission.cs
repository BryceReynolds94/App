using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;

using PagerBuddy.Interfaces;
using System.Threading.Tasks;
using UserNotifications;
using Xamarin.Forms;
using PagerBuddy.Services;

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.iOS.Permission))] //register for dependency service as platform-specific code
namespace PagerBuddy.iOS
{
    class Permission : IiOSPermissions
    {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public async Task checkAlertPermissions(Page currentView, bool forceReprompt = false)
        {
            if (!DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_IOS_NOTIFICATION_PERMISSION, false) || forceReprompt)
            {
                _ = requestNotificationPermission();
                DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_IOS_NOTIFICATION_PERMISSION, true);
            }
        }

        public async void logPermissionSettings()
        {

            UNNotificationSettings settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
            Logger.Debug("Status of notification permissions: Sound: " + settings.SoundSetting + ", Alerts: " + settings.AlertSetting + ", Critical Alerts: " + settings.CriticalAlertSetting);
        }

        public async Task<bool> requestNotificationPermission()
        {

            //https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskcompletionsource-1?view=net-5.0
            Logger.Info("Requesting notification authorisation.");

            UNUserNotificationCenter center = UNUserNotificationCenter.Current;
            UNAuthorizationOptions options = UNAuthorizationOptions.Sound | UNAuthorizationOptions.Alert | UNAuthorizationOptions.CriticalAlert;
            Tuple<bool, NSError> tupleOut = await center.RequestAuthorizationAsync(options);
            if (tupleOut.Item2 != null)
            {
                Logger.Error(tupleOut.Item2.DebugDescription, "Error while requesting notification authorisation.");
            }
            Logger.Debug("Result of authorisation request: " + tupleOut.Item1);
            return tupleOut.Item1;
        }
    }
}