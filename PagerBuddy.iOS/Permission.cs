using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;

using PagerBuddy.Interfaces;
using System.Threading.Tasks;
using UserNotifications;

namespace PagerBuddy.iOS {
    class Permission : IiOSPermissions {

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public void logPermissionSettings() {

            //TODO: Implement permission check
            Logger.Warn("Permission check for iOS not implemented.");
        }

        public async Task<bool> requestNotificationPermission() {
                //TODO: Test this implementation

                //https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskcompletionsource-1?view=net-5.0
                Logger.Info("Requesting notification authorisation.");

                UNUserNotificationCenter center = UNUserNotificationCenter.Current;
                UNAuthorizationOptions options = UNAuthorizationOptions.Sound | UNAuthorizationOptions.Alert | UNAuthorizationOptions.CriticalAlert;
                Tuple<bool, NSError> tupleOut = await center.RequestAuthorizationAsync(options).ConfigureAwait(false);
                if (tupleOut.Item2 != null) {
                    Logger.Error(tupleOut.Item2.DebugDescription, "Error while requesting notification authorisation.");
                    //TODO: IOS Handle this scenario
                }
                Logger.Debug("Result of authorisation request: " + tupleOut.Item1);
                return tupleOut.Item1;
        }
    }
}