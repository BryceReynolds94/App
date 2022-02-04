using Foundation;
using ObjCRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;
using UserNotifications;

namespace PagerBuddy.iOS {
    public class UserNotificationCenterDelegate : UNUserNotificationCenterDelegate {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler) {
            Logger.Info("Received notification while in foreground. Will pass back to system for handling.");

            //Ensure APNS are shown even if in foreground
            //https://iosarchitect.com/show-push-notifications-when-app-running-in-foreground-ios-swift/
            completionHandler(UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.Sound);
        }

    }
}