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

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.iOS.Notification))] //register for dependency service as platform-specific code
namespace PagerBuddy.iOS {
    public class Notification : IiOSNotification {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public void showStandardNotification(string title, string text) {
            //Show the user a local notification (f.e. with update information)

            //TODO: IOS Implementation
            throw new NotImplementedException();
        }

    }
}