using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using FFImageLoading.Forms.Platform;
using FFImageLoading.Svg.Forms;
using Plugin.FirebasePushNotification;
using Firebase.Messaging;
using System.Drawing;
using Android.Content;
using System.Collections.Generic;
using AlarmManagerT.Models;
using AlarmManagerT.Services;

namespace AlarmManagerT.Droid
{
    [Activity(Label = "AlarmManagerT", Icon = "@mipmap/ic_launcher", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            CachedImageRenderer.Init(true); //Addded to enable FFImageLoading
            var ignore = typeof(SvgCachedImage); //Added to enable SVG FFImageLoading

            FirebasePushNotificationManager.ProcessIntent(this, Intent); //Added to enable FirebasePushNotificationPlugin

            LoadApplication(new App(Intent.HasExtra(Alert.EXTRAS.ALERT_FLAG.ToString()), GetAlertFromIntent(Intent)));
            new AndroidNotifications().SetupNotificationChannels(); //Application has to be loaded first
        }

        private Alert GetAlertFromIntent(Intent intent)
        {
            if (intent.HasExtra(Alert.EXTRAS.ALERT_FLAG.ToString())){
                return DataService.deserialiseObject<Alert>(intent.GetStringExtra(Alert.EXTRAS.ALERT_FLAG.ToString()));
            }
            return null;
        }


    }
}