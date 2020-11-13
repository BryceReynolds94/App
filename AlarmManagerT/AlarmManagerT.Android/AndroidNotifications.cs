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
using System.Collections.ObjectModel;

[assembly: Xamarin.Forms.Dependency(typeof(AlarmManagerT.Droid.AndroidNotifications))] //register for dependency service as platform-specific code
namespace AlarmManagerT.Droid {
    public class AndroidNotifications : INotifications {

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static readonly string ALERT_CHANNEL_ID = "Alert Notifications";
        public static readonly string STANDARD_CHANNEL_ID = "Standard Notifications";


        public void showAlertNotification(Alert alert) { //TODO: Include Logo for message
            prepareAlert();

            Intent intent = new Intent(Application.Context, typeof(MainActivity))
                .SetFlags(ActivityFlags.NewTask | ActivityFlags.MultipleTask | ActivityFlags.ExcludeFromRecents)
                .PutExtra(Alert.EXTRAS.ALERT_FLAG.ToString(), DataService.serialiseObject(alert));

            PendingIntent fullScreenIntent = PendingIntent.GetActivity(Application.Context, 0, intent, 0);

            Android.Graphics.Bitmap largePic;
            if (alert.hasPic) {
                largePic = Android.Graphics.BitmapFactory.DecodeFile(DataService.profilePicSavePath(alert.configID));
            } else {
                largePic = Android.Graphics.BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.group_default);
            }

            NotificationCompat.Builder builder = new NotificationCompat.Builder(Application.Context, ALERT_CHANNEL_ID)
                .SetContentTitle(alert.title)
                .SetContentText(alert.text)
                .SetSmallIcon(Resource.Mipmap.ic_launcher) //TODO: Testing
                .SetLargeIcon(largePic)
                .SetPriority(NotificationCompat.PriorityHigh)
                .SetCategory(NotificationCompat.CategoryMessage)
                .SetAutoCancel(true) //cancel notification when tapped
                .SetFullScreenIntent(fullScreenIntent, true)
                .SetStyle(new NotificationCompat.BigTextStyle().BigText(alert.text));

            if (AndroidNavigation.isTelegramInstalled()) {
                builder.SetContentIntent(PendingIntent.GetActivity(Application.Context, 0, AndroidNavigation.getTelegramIntent(alert.chatID), 0));
            }

            NotificationManager manager = NotificationManager.FromContext(Application.Context);
            manager.Notify(new Random().Next(), builder.Build()); //Currently no need to access notification later - so set ID random and forget
        }

        private void prepareAlert() {
            //Disable DND if possible
            NotificationManager manager = NotificationManager.FromContext(Application.Context);
            if (manager.CurrentInterruptionFilter != InterruptionFilter.All && manager.IsNotificationPolicyAccessGranted) {
                Logger.Debug("Clearing DND Filter for alert.");
                manager.SetInterruptionFilter(InterruptionFilter.All);
            }

            //Set Max Volume if possible
            AudioManager audioManager = AudioManager.FromContext(Application.Context);
            if (!audioManager.IsVolumeFixed) //do not bother with devices that do not have volume control
            {
                try {
                    audioManager.SetStreamVolume(Stream.Notification, audioManager.GetStreamMaxVolume(Stream.Notification), 0); //TODO: Check if stream is correct
                } catch (Exception e) {
                    Logger.Warn(e, "Could not set volume. Probably due to insufficient permissions");
                }
            }
        }

        public void showStandardNotification(string title, string text) {
            NotificationCompat.Builder builder = new NotificationCompat.Builder(Application.Context, STANDARD_CHANNEL_ID)
                .SetContentTitle(title)
                .SetContentText(text)
                .SetSmallIcon(Resource.Mipmap.ic_launcher)
                .SetPriority(NotificationCompat.PriorityDefault);

            NotificationManager manager = NotificationManager.FromContext(Application.Context);
            manager.Notify(new Random().Next(), builder.Build()); //Currently no need to access notification later - so set ID random and forget
        }

        public void addNotificationChannel(AlertConfig alertConfig) {
            Logger.Debug("Setting up notification channel for config: " + alertConfig.triggerGroup.name);

            NotificationChannel notificationChannel = new NotificationChannel(alertConfig.id, alertConfig.triggerGroup.name, NotificationImportance.High) {
                LightColor = Color.Red.ToArgb(),
                LockscreenVisibility = NotificationVisibility.Public,
                Group = ALERT_CHANNEL_ID
            };
            notificationChannel.EnableLights(true);
            notificationChannel.SetVibrationPattern(new long[] { 0, 1000, 500, 100, 100, 1000, 500, 100, 100, 1000 }); //TODO: Check this vibration pattern
            notificationChannel.EnableVibration(true);

            NotificationManager notificationManager = NotificationManager.FromContext(Application.Context);
            notificationManager.CreateNotificationChannel(notificationChannel);
        }

        public void removeNotificationChannel(AlertConfig alertConfig) {
            Logger.Debug("Deleting notification channel for config: " + alertConfig.triggerGroup.name);

            NotificationManager notificationManager = NotificationManager.FromContext(Application.Context);
            notificationManager.DeleteNotificationChannel(alertConfig.id);
        }

        public void SetupNotificationChannels() {
            //TODO: Handle default notification channel for FCM

            NotificationChannelGroup channelGroup = new NotificationChannelGroup(ALERT_CHANNEL_ID, Resources.AppResources.Android_AndroidNotifications_AlertChannel_Title) {
                Description = Resources.AppResources.Android_AndroidNotifications_AlertChannel_Description
            };

            string standardName = "App Information"; //TODO: RBF
            string standardDescription = "Notifications relevant to general app behaviour and updates."; //TODO: RBF
            NotificationChannel standardChannel = new NotificationChannel(AndroidNotifications.STANDARD_CHANNEL_ID, standardName, NotificationImportance.Default) {
                Description = standardDescription

            }; //TODO: Possibly add further attributes to standard notifications

            NotificationManager notificationManager = NotificationManager.FromContext(Application.Context);
            notificationManager.CreateNotificationChannel(standardChannel);
            notificationManager.CreateNotificationChannelGroup(channelGroup);

            Collection<string> configList = DataService.getConfigList(); 
            foreach(string config in configList) {
                addNotificationChannel(DataService.getAlertConfig(config));
            }
        }



    }

}