using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PagerBuddy.Interfaces;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using AndroidX.Core.App;
using Android.Media;
using System.Drawing;
using PagerBuddy.Models;
using PagerBuddy.Services;
using System.Collections.ObjectModel;
using PagerBuddy.Views;
using Xamarin.Essentials;
using Uri = Android.Net.Uri;
using Java.IO;
using System.Threading.Tasks;
using System.IO;

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.Droid.Notifications))] //register for dependency service as platform-specific code
namespace PagerBuddy.Droid {
    public class Notifications : IAndroidNotifications {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static readonly string ALERT_CHANNEL_ID = "de.bartunik.pagerbuddy.alert";
        public static readonly string STANDARD_CHANNEL_ID = "de.bartunik.pagerbuddy.standard";

        
        public void showAlertNotification(Alert alert, int percentVolume) {
            prepareAlert(percentVolume/100);

            Intent intent = new Intent(Application.Context, typeof(MainActivity))
                .SetFlags(ActivityFlags.NewTask | ActivityFlags.MultipleTask | ActivityFlags.ExcludeFromRecents)
                .PutExtra(Alert.EXTRAS.ALERT_FLAG.ToString(), DataService.serialiseObject(alert));

            PendingIntent fullScreenIntent = PendingIntent.GetActivity(Application.Context, 0, intent, PendingIntentFlags.Immutable);

            Android.Graphics.Bitmap largePic;
            if (alert.hasPic) {
                largePic = Android.Graphics.BitmapFactory.DecodeFile(DataService.profilePicSavePath(alert.configID));
            } else {
                largePic = Android.Graphics.BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.group_default);
            }

            long timestamp = (long) alert.timestamp.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds; //Android uses Unix time

            Notification.Builder builder = new Notification.Builder(Application.Context, alert.configID)
                .SetContentTitle(alert.title)
                .SetContentText(alert.description)
                .SetSmallIcon(Resource.Drawable.notification_icon) //use simplified xml vector
                .SetColor(Resource.Color.colorPrimary) //set app color for small notification icon
                .SetLargeIcon(largePic) //group pic 
                .SetCategory(Notification.CategoryCall) //category for message classification 
                .SetAutoCancel(true) //cancel notification when tapped
                .SetFullScreenIntent(fullScreenIntent, true)
                .SetWhen(timestamp) 
                .SetShowWhen(true)
                .SetStyle(new Notification.BigTextStyle().BigText(alert.description)); //extend message on tap

            if (new Navigation().isTelegramInstalled()) {
                PendingIntent chatIntent = PendingIntent.GetActivity(Application.Context, 0, Navigation.getTelegramIntent(alert.chatID, alert.peerType), 0);
                builder.SetContentIntent(chatIntent);
            } else {
                builder.SetContentIntent(fullScreenIntent);
            }

            Notification notification = builder.Build();
            notification.Flags |= NotificationFlags.Insistent; //repeat sound untill acknowledged

            NotificationManager manager = NotificationManager.FromContext(Application.Context);
            manager.Notify((int) alert.chatID, notification);
        }

        public void closeNotification(int notificationID) {
            NotificationManager manager = NotificationManager.FromContext(Application.Context);
            manager.Cancel(notificationID);
        }

        private void prepareAlert(float volumeFactor) {
            //Disable DND if possible
            NotificationManager manager = NotificationManager.FromContext(Application.Context);
            if (manager.CurrentInterruptionFilter != InterruptionFilter.All && manager.IsNotificationPolicyAccessGranted) {
                Logger.Debug("Clearing DND Filter for alert.");
                manager.SetInterruptionFilter(InterruptionFilter.All);
            }

            //Set  Volume if possible
            AudioManager audioManager = AudioManager.FromContext(Application.Context);
            if (!audioManager.IsVolumeFixed) //do not bother with devices that do not have volume control
            {
                try {
                    int NminVol = audioManager.GetStreamMinVolume(Android.Media.Stream.Notification);
                    int NmaxVol = audioManager.GetStreamMaxVolume(Android.Media.Stream.Notification);
                    int NVolIndex = (int) Math.Round((NmaxVol - NminVol) * volumeFactor + NminVol);
                    audioManager.SetStreamVolume(Android.Media.Stream.Notification, NVolIndex, 0);

                    int RminVol = audioManager.GetStreamMinVolume(Android.Media.Stream.Ring);
                    int RmaxVol = audioManager.GetStreamMaxVolume(Android.Media.Stream.Ring);
                    int RVolIndex = (int) Math.Round((RmaxVol - RminVol) * volumeFactor + RminVol);
                    audioManager.SetStreamVolume(Android.Media.Stream.Ring, RVolIndex, 0); //also have to set ringer high for Samsung devices
                } catch (Exception e) {
                    Logger.Warn(e, "Could not set volume. Probably due to insufficient permissions");
                }
            }
        }

        public void showStandardNotification(string title, string text) {
            Intent intent = new Intent(Application.Context, typeof(MainActivity));
            PendingIntent pendingIntent = PendingIntent.GetActivity(Application.Context, 0, intent, 0);

            Notification.Builder builder = new Notification.Builder(Application.Context, STANDARD_CHANNEL_ID)
                .SetContentTitle(title)
                .SetContentText(text)
                .SetSmallIcon(Resource.Drawable.notification_icon)
                .SetCategory(Notification.CategoryStatus)
                .SetColor(Resource.Color.colorPrimary) //set app color for small notification icon
                .SetStyle(new Notification.BigTextStyle().BigText(text))//extend message on tap
                .SetContentIntent(pendingIntent);


            NotificationManager manager = NotificationManager.FromContext(Application.Context);
            manager.Notify(new Random().Next(), builder.Build()); //Currently no need to access notification later - so set ID random and forget
        }

        public Action playChannelRingtone(string alertConfigID) {
            NotificationManager notificationManager = NotificationManager.FromContext(Application.Context);
            NotificationChannel channel = notificationManager.GetNotificationChannel(alertConfigID);

            if(channel == null) {
                Logger.Warn("Could not find notification channel for alert config.");
                return null;
            }

            Uri sound = channel.Sound;
            Ringtone ringtone = RingtoneManager.GetRingtone(Application.Context, sound);
            ringtone.Looping = true;
            ringtone.Play();

            Action stopAction = new Action(() => {
                if (ringtone != null && ringtone.IsPlaying) {
                    ringtone.Stop();
                }
                ringtone?.Dispose();
                ringtone = null;
            });

            return stopAction;
        }

        public void showToast(string message) {
            Toast toast = Toast.MakeText(Application.Context, message, ToastLength.Long);
            toast.Show();
        }

        public void addNotificationChannel(AlertConfig alertConfig) {
            Logger.Debug("Setting up notification channel for config: " + alertConfig.readableFullName);

            NotificationChannel notificationChannel = new NotificationChannel(alertConfig.id, alertConfig.readableFullName, NotificationImportance.High) {
                LightColor = Color.Red.ToArgb(),
                LockscreenVisibility = NotificationVisibility.Public,
                Group = ALERT_CHANNEL_ID
            };
            notificationChannel.EnableLights(true);
            notificationChannel.SetVibrationPattern(new long[] { 0, 100, 100, 1010, 800 });
            notificationChannel.EnableVibration(true);

            Uri fileUri = new Uri.Builder()
                .Scheme(ContentResolver.SchemeAndroidResource)
                .Authority(AppInfo.PackageName)
                .Path(Resource.Raw.pagerbuddy_sound.ToString()).Build();

            notificationChannel.SetSound(fileUri, new AudioAttributes.Builder().SetUsage(AudioUsageKind.NotificationCommunicationInstant).Build());

            NotificationManager notificationManager = NotificationManager.FromContext(Application.Context);
            notificationManager.CreateNotificationChannel(notificationChannel);
        }

        public void removeNotificationChannel(AlertConfig alertConfig) {
            Logger.Debug("Deleting notification channel for config: " + alertConfig.readableFullName);

            NotificationManager notificationManager = NotificationManager.FromContext(Application.Context);
            notificationManager.DeleteNotificationChannel(alertConfig.id);
        }

        public void UpdateNotificationChannels(Collection<AlertConfig> configList) {
            Collection<string> idList = new Collection<string>();
            foreach(AlertConfig config in configList) {
                addNotificationChannel(config);
                idList.Add(config.id);
            }

            //Clear possibly remaining old channels
            NotificationManager notificationManager = NotificationManager.FromContext(Application.Context);
            foreach (NotificationChannel channel in notificationManager.NotificationChannels) {
                if (channel.Group != null && channel.Group.Equals(ALERT_CHANNEL_ID) && !idList.Contains(channel.Id)) {
                    notificationManager.DeleteNotificationChannel(channel.Id);
                }
            }
        }

        public void SetupNotificationChannels() {

            NotificationChannelGroup channelGroup = new NotificationChannelGroup(ALERT_CHANNEL_ID, Resources.AppResources.Android_AndroidNotifications_AlertChannel_Title);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.P) { //Channel group description only added in API 28
                channelGroup.Description = Resources.AppResources.Android_AndroidNotifications_AlertChannel_Description;
            }

            string standardName = Resources.AppResources.Android_AndroidNotifications_StandardChannel_Title;
            string standardDescription = Resources.AppResources.Android_AndroidNotifications_StandardChannel_Description;
            NotificationChannel standardChannel = new NotificationChannel(STANDARD_CHANNEL_ID, standardName, NotificationImportance.Default) {
                Description = standardDescription

            };

            NotificationManager notificationManager = NotificationManager.FromContext(Application.Context);
            notificationManager.CreateNotificationChannel(standardChannel);
            notificationManager.CreateNotificationChannelGroup(channelGroup);

            Collection<string> configIDList = DataService.getConfigList();
            Collection<AlertConfig> configList = new Collection<AlertConfig>();
            foreach(string config in configIDList) {
                AlertConfig alertConfig = DataService.getAlertConfig(config, null);
                if (alertConfig != null) {
                    configList.Add(alertConfig);
                }
            }

            UpdateNotificationChannels(configList);
        }

        public void RefreshToken() {
            new FCMMessagingService().RefreshToken();
        }


    }

}