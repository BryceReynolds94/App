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
            //This is called to setup a notification channel, each channel allows for an own notification setting (sound, vibration, etc.)
            //Ideally a system interface for managing notification settings for each alert channel should be used

            //TODO: IOS Implementation

            //TODO: IOS RBF
            requestNotificationAuthorisation().Wait();
        }

        public void closeNotification(int notificationID) {
            //Dismiss a previously shown notification

            //TODO: IOS Implementation
            throw new NotImplementedException();
        }

        public void removeNotificationChannel(AlertConfig config) {
            //Remove a notification channel (complement to addNotificationChannel)

            //TODO: IOS Implementation
            throw new NotImplementedException();
        }

        public void showAlertNotification(Alert alert) {
            //Show the user an alert notification
            //Possibly this is only done on APNS events and is not triggered locally from App

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
            //Show the user a local notification (f.e. with update information)

            //TODO: IOS Implementation
            throw new NotImplementedException();
        }

        public void UpdateNotificationChannels(Collection<AlertConfig> configList) {
            //Fill notification channels from prvoided list 

            //TODO: IOS Implementation

            throw new NotImplementedException();
        }

        private async Task<bool> requestNotificationAuthorisation() {
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