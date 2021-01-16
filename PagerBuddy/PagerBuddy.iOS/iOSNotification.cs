using Foundation;
using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using UserNotifications;

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.iOS.iOSNotification))] //register for dependency service as platform-specific code
namespace PagerBuddy.iOS {
    public class iOSNotification : INotifications{

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public void addNotificationChannel(AlertConfig config) {
            //TODO: IOS Implementation

            //TODO: RBF
            requestNotificationAuthorisation().Wait();
        }

        public void closeNotification(int notificationID) {
            //TODO: IOS Implementation
            throw new NotImplementedException();
        }

        public void removeNotificationChannel(AlertConfig config) {
            //TODO: IOS Implementation
            throw new NotImplementedException();
        }

        public void showAlertNotification(Alert alert) {
            //TODO: IOS Implementation
            throw new NotImplementedException();
        }

        public void showStandardNotification(string title, string text) {
            //TODO: IOS Implementation
            throw new NotImplementedException();
        }

        private async Task<bool> requestNotificationAuthorisation() {
            Logger.Info("Requesting notification authorisation.");
            UNUserNotificationCenter center = UNUserNotificationCenter.Current;

            UNAuthorizationOptions options = UNAuthorizationOptions.Sound | UNAuthorizationOptions.Alert | UNAuthorizationOptions.CriticalAlert;
            Tuple<bool, NSError> result = await center.RequestAuthorizationAsync(options);

            if(result.Item2 != null) {
                Logger.Error(result.Item2.DebugDescription, "Error while requesting notification authorisation.");
                //TODO: Handle this scenario
            }

            Logger.Debug("Result of authorisation request: " + result.Item1);
            return result.Item1;
        }
    }
}