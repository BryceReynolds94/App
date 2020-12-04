using Foundation;
using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.iOS.iOSNotification))] //register for dependency service as platform-specific code
namespace PagerBuddy.iOS {
    public class iOSNotification : INotifications{
        public void addNotificationChannel(AlertConfig config) {
            //TODO: IOS Implementation
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void showStandardNotification(string title, string text) {
            //TODO: IOS Implementation
            throw new NotImplementedException();
        }
    }
}