using Foundation;
using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;
using UserNotifications;

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.iOS.Notifications))] //register for dependency service as platform-specific code
namespace PagerBuddy.iOS {
    public class Notifications : IiOSNotifications {

        private static readonly string ALERT_ID = "de.bartunik.pagerbuddy.alert";

        public void showToast(string message) {

            UIAlertController alert = UIAlertController.Create(null, message, UIAlertControllerStyle.Alert);
            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alert, true, null);
            NSTimer.CreateScheduledTimer(2, (obj) => {
                alert?.DismissViewController(true, null);
            });
        }

        public void showAlertNotification(Alert alert, int percentVolume) {

            UNMutableNotificationContent content = new UNMutableNotificationContent {
                Title = alert.title,
                Body = alert.description,
                InterruptionLevel = UNNotificationInterruptionLevel.Critical,
                Sound = UNNotificationSound.GetCriticalSound("pagerbuddy_sound_long.wav")
            };

            UNNotificationTrigger trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(5, false);

            UNNotificationRequest request = UNNotificationRequest.FromIdentifier(ALERT_ID, content, trigger);


            UNUserNotificationCenter center = UNUserNotificationCenter.Current;
            center.AddNotificationRequestAsync(request);
        }
    }
}