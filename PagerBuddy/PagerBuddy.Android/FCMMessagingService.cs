using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Firebase.Messaging;
using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PagerBuddy.Droid {

    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT"})]
    public class FCMMessagingService : FirebaseMessagingService{

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public override void OnMessageReceived(RemoteMessage p0) {
            if (!Xamarin.Forms.Forms.IsInitialized) //If Forms is initalised we do not have to handle notification here
            {
                Logger.Debug("Notification received in background. Initialising Platform.");
                Xamarin.Essentials.Platform.Init(this.Application); //We need to init Essentials from killed state to use preference storage
                Xamarin.Forms.Forms.Init(Application.Context, null); //We need to make sure Xamarin.Forms is initialised when notifications are received in killed state  
            }
            MessagingService.FirebaseMessage(this, p0.Data, p0.SentTime);
        }

        public override void OnNewToken(string p0) {
            if (!Xamarin.Forms.Forms.IsInitialized) //If Forms is initalised we do not have to handle notification here
{
                Logger.Debug("Token update received in background. Initialising Platform.");
                Xamarin.Essentials.Platform.Init(this.Application); //We need to init Essentials from killed state to use preference storage
                Xamarin.Forms.Forms.Init(Application.Context, null); //We need to make sure Xamarin.Forms is initialised when notifications are received in killed state  
            }
            MessagingService.FirebaseTokenRefresh(this, p0).Wait();
        }
    }
}