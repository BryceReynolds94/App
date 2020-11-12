using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlarmManagerT.Services;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Plugin.FirebasePushNotification;


//https://github.com/CrossGeeks/FirebasePushNotificationPlugin/blob/master/docs/GettingStarted.md
//TODO: Do FCM setup in iOS
namespace AlarmManagerT.Droid
{
    [Application]
    public class MainApplication : Application
    {
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
                    Xamarin.Forms.Forms.Init(Application.Context, null); //We need to make sure Xamarin.Forms is initialised when notifications are received in killed state
                    MessagingService.BackgroundFirebaseMessage(this, p);
                }

            };

        }
    }

}
 