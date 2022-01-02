using Foundation;
using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.iOS.Navigation))] //register for dependency service as platform-specific code
namespace PagerBuddy.iOS {
    class Navigation : IiOSNavigation {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public bool isTelegramInstalled() {

            //https://www.hackingwithswift.com/example-code/system/how-to-check-whether-your-other-apps-are-installed

            //TODO: iOS TESTING
            bool telegramInstalled = false;
            try {
                telegramInstalled = UIApplication.SharedApplication.CanOpenUrl(new NSUrl("telegram://test"));
            } catch(Exception e) {
                Logger.Warn(e, "Error probing for Telegram installation.");
            }
            return telegramInstalled;
        }

    }
} 