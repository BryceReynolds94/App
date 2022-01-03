using Firebase.CloudMessaging;
using Foundation;
using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;

namespace PagerBuddy.iOS {
    class MessagingDelegate : Firebase.CloudMessaging.MessagingDelegate {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public override void DidReceiveRegistrationToken(Messaging messaging, string fcmToken) {
            if (!Xamarin.Forms.Forms.IsInitialized) {
                Logger.Debug("Token update received in background. Initialising Platform.");
                Xamarin.Forms.Forms.Init(); //We need to make sure Xamarin.Forms is initialised when notifications are received in killed state  
            }
            MessagingService.TokenRefresh(fcmToken);
        }
    }
}