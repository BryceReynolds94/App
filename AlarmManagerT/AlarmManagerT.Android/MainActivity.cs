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

namespace AlarmManagerT.Droid
{
    [Activity(Label = "AlarmManagerT", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
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

            LoadApplication(new App());

            FirebasePushNotificationManager.ProcessIntent(this, Intent); //Added to enable FirebasePushNotificationPlugin

            SetupNotificationChannels(); //TODO: RBF
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public void SetupNotificationChannels()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.
                return;
            }

            string alertName = "Alert Notifications"; //TODO: RBF
            string alertDescription = "Configured alert notifications."; //TODO: RBF
            NotificationChannel alertChannel = new NotificationChannel(AndroidNotifications.ALERT_CHANNEL_ID, alertName, NotificationImportance.High)
            {
                Description = alertDescription,
                LightColor = Color.Red.ToArgb(),
                LockscreenVisibility = NotificationVisibility.Private
            };
            alertChannel.EnableLights(true);
            alertChannel.EnableVibration(true);
            alertChannel.SetBypassDnd(true); //TODO: Check - this probably does not work without further settings https://developer.android.com/reference/android/app/NotificationChannel#canBypassDnd()
            //TODO: alertChannel.setSound()

            //TODO: Possibly set vibration pattern


            //TODO: Implement BypassDND and check canBypassDND()

            string standardName = "App Information"; //TODO: RBF
            string standardDescription = "Notifications relevant to general app behaviour and updates."; //TODO: RBF
            NotificationChannel standardChannel = new NotificationChannel(AndroidNotifications.STANDARD_CHANNEL_ID, standardName, NotificationImportance.Default)
            {
                Description = standardDescription

            }; //TODO: Possibly add further attributes to standard notifications

            NotificationManager notificationManager = (NotificationManager) GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(alertChannel);
            notificationManager.CreateNotificationChannel(standardChannel);
        }


    }
}