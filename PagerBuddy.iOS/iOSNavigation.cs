using Foundation;
using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.iOS.iOSNavigation))] //register for dependency service as platform-specific code
namespace PagerBuddy.iOS {
    class iOSNavigation : INavigation {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public bool isTelegramInstalled() {
            //This should check if a Telegram client is installed on the device. The suer is prompted to install Telegram if no client is found.


            //TODO: IOS Implementation
            //https://stackoverflow.com/questions/41545283/how-to-check-app-is-installed-or-not-in-phone
            return false;
        }

        public void navigateNotificationPolicyAccess() {
            //This is only used in Android
            
            //TODO: IOS Check if equivalent call for iOS necessary (https://developer.apple.com/documentation/usernotifications/asking_permission_to_use_notifications)
            //We have to check all necessary permission for iOS and request them seperately

            Logger.Error("NavigateNotificationPolicyAccess called on iOS. This should never happen.");
        }

        public void navigateNotificationSettings() {
            //The system should handle notification settings (sound, vibration, etc...) if possible. On Android this is done by sending the user to an appropriate system settings dialog
            //Similar behaviour would be good for iOS, but should confer with typical iOS behaviour.

            //TODO: IOS Implementation
            Logger.Error("not implemented");
            throw new NotImplementedException();
        }

        public void navigateTelegramChat(int chatID, TelegramPeer.TYPE type) {
            //On a confirmed alert event the user is sent to the appropriate telegram chat.
            //Depending on how notifications are implemented in iOS this may not be possible/necessary

            //TODO: IOS Implementation 
            Logger.Error("not implemented");
            throw new NotImplementedException();
        }

        public void quitApplication() {
            //This cannot be done in iOS - only Home button is valid to close app
            //TODO: IOS Check other implementation of closing alert (probably no alert screen at all with iOS)
        }

        void INavigation.logPermissionSettings() {
            //TODO: IOS Implementation

            //This should check the current status of all necessary permissions, reprompt the user if necessary and possible and log the permission status

            Logger.Error("not implemented");
            throw new NotImplementedException();
        }

        void INavigation.navigateDozeExempt() {
            //This is only used in android
            Logger.Error("NavigateDozeExempt called on iOS. This should never happen.");
        }

        void INavigation.navigateHuaweiPowerException() {
            //This is only used in android
            Logger.Error("NavigateHuaweiPowerException called on iOS. This should never happen.");
        }
    }
} 