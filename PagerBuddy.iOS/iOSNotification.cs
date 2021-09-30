using Foundation;
using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using UserNotifications;

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.iOS.iOSNotification))] //register for dependency service as platform-specific code
namespace PagerBuddy.iOS {
    public class iOSNotification : INotifications {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public void addNotificationChannel(AlertConfig config) {
            //TODO: IOS Implementation

            //TODO: IOS RBF
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

            UNMutableNotificationContent content = new UNMutableNotificationContent();
            content.Title = alert.title;
            content.Subtitle = "Blabla";
            content.Body = alert.text;

            UNTimeIntervalNotificationTrigger trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(1, false);

            UNNotificationRequest request = UNNotificationRequest.FromIdentifier("someID", content, trigger); //TODO: IOS RBF
            UNUserNotificationCenter center = UNUserNotificationCenter.Current;
            center.AddNotificationRequest(request, (err) => {
                if(err != null) {
                    Logger.Error("Could not register notification.", err);
                }
            });

        }

        public void showStandardNotification(string title, string text) {
            //TODO: IOS Implementation
            throw new NotImplementedException();
        }

        public void UpdateNotificationChannels(Collection<AlertConfig> configList) {
            throw new NotImplementedException();
        }

        private async Task<bool> requestNotificationAuthorisation() {
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