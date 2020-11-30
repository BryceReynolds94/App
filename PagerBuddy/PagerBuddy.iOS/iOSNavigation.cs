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
        public void navigateNotificationPolicyAccess() {
            //TODO: IOS Implementation
            throw new NotImplementedException();
        }

        public void navigateNotificationSettings() {
            //TODO: IOS Implementation
            throw new NotImplementedException();
        }

        public void navigateShareFile(string fileName) {
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