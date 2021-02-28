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

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public override void OnMessageReceived(RemoteMessage p0) {
            //Caution: If called in background while dozing the app will have max. 10s to react to message!

            if (!Xamarin.Forms.Forms.IsInitialized) //If Forms is initalised we do not have to handle notification here
            {
                Logger.Debug("Notification received in background. Initialising Platform.");
                Xamarin.Essentials.Platform.Init(this.Application); //We need to init Essentials from killed state to use preference storage
                Xamarin.Forms.Forms.Init(Application.Context, null); //We need to make sure Xamarin.Forms is initialised when notifications are received in killed state  
            }

            DateTime sentTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(p0.SentTime).ToLocalTime(); //Unix base time -- we need local time for alert time comparison

            Logger.Debug(string.Format("Got FCM message with prio {0} (sent as {1}) at {2} (sent at {3}).", p0.Priority, p0.OriginalPriority, DateTime.Now.ToString("HH:mm:ss"), sentTime.ToString("HH:mm:ss")));
            MessagingService.FirebaseMessage(p0.Data, sentTime);
        }

        public override void OnNewToken(string p0) {
            if (!Xamarin.Forms.Forms.IsInitialized) //If Forms is initalised we do not have to handle notification here
{
                Logger.Debug("Token update received in background. Initialising Platform.");
                Xamarin.Essentials.Platform.Init(this.Application); //We need to init Essentials from killed state to use preference storage
                Xamarin.Forms.Forms.Init(Application.Context, null); //We need to make sure Xamarin.Forms is initialised when notifications are received in killed state  
            }
            MessagingService.FirebaseTokenRefresh(p0).Wait();
        }
    }
}