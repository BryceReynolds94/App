using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlarmManagerT.Interfaces;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Android.Support.V4.App; //needed for compat notifications https://docs.microsoft.com/en-us/xamarin/android/app-fundamentals/notifications/local-notifications-walkthrough
using Android.Media;
using System.Drawing;
using AlarmManagerT.Models;
using AlarmManagerT.Services;

[assembly: Xamarin.Forms.Dependency(typeof(AlarmManagerT.Droid.AndroidNotifications))] //register for dependency service as platform-specific code
namespace AlarmManagerT.Droid {
    public class AndroidNotifications : INotifications {
        public static readonly string ALERT_CHANNEL_ID = "Alert Notifications";
        public static readonly string STANDARD_CHANNEL_ID = "Standard Notifications";


        public void showAlertNotification(Alert alert) { //TODO: Include Logo for message
            prepareAlert();

            Intent intent = new Intent(Application.Context, typeof(MainActivity))
                .SetFlags(ActivityFlags.NewTask | ActivityFlags.MultipleTask | ActivityFlags.ExcludeFromRecents)
                .PutExtra(Alert.EXTRAS.ALERT_FLAG.ToString(), DataService.serialiseObject(alert)); //TODO: Check this

            PendingIntent fullScreenIntent = PendingIntent.GetActivity(Application.Context, 0, intent, 0);


            NotificationCompat.Builder builder = new NotificationCompat.Builder(Application.Context, ALERT_CHANNEL_ID)
                .SetContentTitle(alert.title)
                .SetSmallIcon(Resource.Drawable.ic_launcher_foreground) //TODO: Adjust Logo
                .SetContentText(alert.text)
                .SetPriority(NotificationCompat.PriorityHigh)
                .SetCategory(NotificationCompat.CategoryMessage)
                .SetFullScreenIntent(fullScreenIntent, true);

            //TODO: Add full-screen intent https://developer.android.com/training/notify-user/time-sensitive

            NotificationManager manager = NotificationManager.FromContext(Application.Context);
            manager.Notify(new Random().Next(), builder.Build()); //Currently no need to access notification later - so set ID random and forget

        }

        private void prepareAlert() {
            //Disable DND if possible
            NotificationManager manager = NotificationManager.FromContext(Application.Context);
            if (manager.CurrentInterruptionFilter != InterruptionFilter.All && manager.IsNotificationPolicyAccessGranted) {
                manager.SetInterruptionFilter(InterruptionFilter.All);
            }

            //Set Max Volume if possible
            AudioManager audioManager = AudioManager.FromContext(Application.Context);
            if (!audioManager.IsVolumeFixed) //do not bother with devices that do not have volume control
            {
                try {
                    audioManager.SetStreamVolume(Stream.Notification, audioManager.GetStreamMaxVolume(Stream.Notification), 0); //TODO: Check if stream is correct
                } catch (Exception e) {
                    //We do not have the required permissions to change volume out of DND - forget
                    //TODO: Log 
                }
            }
        }

        public void showStandardNotification(string title, string text) {
            NotificationCompat.Builder builder = new NotificationCompat.Builder(Application.Context, STANDARD_CHANNEL_ID)
                .SetContentTitle(title)
                .SetSmallIcon(Resource.Drawable.ic_launcher_foreground) //TODO: Adjust Logo
                .SetContentText(text)
                .SetPriority(NotificationCompat.PriorityDefault);

            NotificationManager manager = NotificationManager.FromContext(Application.Context);
            manager.Notify(new Random().Next(), builder.Build()); //TODO: Possibly change to access/clear notification by ID
        }

        public void SetupNotificationChannels() {
            //TODO: Intorduce notification groups
            //TODO: Handle default notification channel for FCM

            if (Build.VERSION.SdkInt < BuildVersionCodes.O) {
                return;
            }

            string alertName = "Alert Notifications"; //TODO: RBF
            string alertDescription = "Configured alert notifications."; //TODO: RBF
            NotificationChannel alertChannel = new NotificationChannel(AndroidNotifications.ALERT_CHANNEL_ID, alertName, NotificationImportance.High) {
                Description = alertDescription,
                LightColor = Color.Red.ToArgb(),
                LockscreenVisibility = NotificationVisibility.Private
            };
            alertChannel.EnableLights(true);
            alertChannel.SetVibrationPattern(new long[] { 0, 1000, 500, 100, 100, 1000, 500, 100, 100, 1000 }); //TODO: Check this vibration pattern
            alertChannel.EnableVibration(true);
            //TODO: alertChannel.setSound()

            string standardName = "App Information"; //TODO: RBF
            string standardDescription = "Notifications relevant to general app behaviour and updates."; //TODO: RBF
            NotificationChannel standardChannel = new NotificationChannel(AndroidNotifications.STANDARD_CHANNEL_ID, standardName, NotificationImportance.Default) {
                Description = standardDescription

            }; //TODO: Possibly add further attributes to standard notifications

            NotificationManager notificationManager = NotificationManager.FromContext(Application.Context);
            notificationManager.CreateNotificationChannel(alertChannel);
            notificationManager.CreateNotificationChannel(standardChannel);
        }



    }

}