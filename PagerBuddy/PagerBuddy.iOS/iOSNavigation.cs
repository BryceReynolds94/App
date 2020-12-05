using Foundation;
using PagerBuddy.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.iOS.iOSNavigation))] //register for dependency service as platform-specific code
namespace PagerBuddy.iOS {
    class iOSNavigation : INavigation {
        public bool isTelegramInstalled() {
            //TODO: IOS Implementation
            //https://stackoverflow.com/questions/41545283/how-to-check-app-is-installed-or-not-in-phone
            throw new NotImplementedException();
        }

        public void navigateNotificationPolicyAccess() {
            //TODO: IOS Implementation
            throw new NotImplementedException();
        }

        public void navigateNotificationSettings() {
            //TODO: IOS Implementation
            throw new NotImplementedException();
        }

        public void navigateTelegramChat(int chatID) {
            //TODO: IOS Implementation
            throw new NotImplementedException();
        }

        public void quitApplication() {
            //TODO: IOS Implementation
            throw new NotImplementedException();
        }
    }
}