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

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public bool isTelegramInstalled() {
            //TODO: IOS Implementation
            //https://stackoverflow.com/questions/41545283/how-to-check-app-is-installed-or-not-in-phone
            throw new NotImplementedException();
        }

        public void navigateNotificationPolicyAccess() {
            //This is only used in android
            //TODO: IOS Check if equivalent call for iOS necessary (https://developer.apple.com/documentation/usernotifications/asking_permission_to_use_notifications)
            Logger.Error("NavigateNotificationPolicyAccess called on iOS. This should never happen.");
            throw new NotImplementedException();
        }

        public void navigateNotificationSettings() {
            //TODO: IOS Implementation
            throw new NotImplementedException();
        }

        public void navigateTelegramChat(int chatID, TelegramPeer.TYPE type) {
            //TODO: IOS Implementation 
            throw new NotImplementedException();
        }

        public void quitApplication() {
            //This cannot be done in iOS - only Home button is valid to close app
            //TODO: IOS Check other implementation of closing alert (probably no alert screen at all with iOS)
        }
    }
} 