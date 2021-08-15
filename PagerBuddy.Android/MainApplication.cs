using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PagerBuddy.Services;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Plugin.FirebasePushNotification;


//https://github.com/CrossGeeks/FirebasePushNotificationPlugin/blob/master/docs/GettingStarted.md
//TODO iOS: Do FCM setup in iOS
namespace PagerBuddy.Droid
{
    [Application]
    public class MainApplication : Application
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public MainApplication(IntPtr handle, JniHandleOwnership transer) : base(handle, transer)
        {
        }
        public override void OnCreate()
        {
            base.OnCreate();
            FirebasePushNotificationManager.Initialize(this, false, false);

            //Handle notification when app is closed here
            CrossFirebasePushNotification.Current.OnNotificationReceived += (s, p) =>
            {
                if (!Xamarin.Forms.Forms.IsInitialized) //If Forms is initalised we do not have to handle notification here
                {
                    Logger.Debug("Notification received in background. Initialising Platform.");
                    Xamarin.Essentials.Platform.Init(this); //We need to init Essentials from killed state to use preference storage
                    Xamarin.Forms.Forms.Init(Application.Context, null); //We need to make sure Xamarin.Forms is initialised when notifications are received in killed state
                    
                    MessagingService.BackgroundFirebaseMessage(this, p);
                }

            };

            CrossFirebasePushNotification.Current.OnTokenRefresh += (s, args) =>
            {
                if (!Xamarin.Forms.Forms.IsInitialized) {
                    Logger.Info("Firebase token was updated, TOKEN: {0}", args.Token);
                    MessagingService.BackgroundFirebaseTokenRefresh(this, args.Token);
                }
            };

        }
    }

}
 