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
using Android.Provider;
using Xamarin.Essentials;
using Android.Content.PM;

[assembly: Xamarin.Forms.Dependency(typeof(AlarmManagerT.Droid.AndroidNavigation))] //register for dependency service as platform-specific code
namespace AlarmManagerT.Droid
{
    class AndroidNavigation : INavigation
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public void navigateNotificationSettings()
        {
            Intent intent = new Intent(Settings.ActionChannelNotificationSettings);
            intent.PutExtra(Settings.ExtraAppPackage, Application.Context.PackageName);
            intent.PutExtra(Settings.ExtraChannelId, AndroidNotifications.ALERT_CHANNEL_ID); //TODO: Possibly change this to group notifications
            Platform.CurrentActivity.StartActivity(intent);
        }

        public void navigateNotificationPolicyAccess()
        {
            Intent intent = new Intent(Settings.ActionNotificationPolicyAccessSettings);
            intent.PutExtra(Settings.ExtraAppPackage, Application.Context.PackageName);
            Platform.CurrentActivity.StartActivity(intent);
        }

        public void navigateShare(string message)
        {
            Intent intent = new Intent()
                .SetAction(Intent.ActionSend)
                .PutExtra(Intent.ExtraText, message)
                .SetType("text/plain");

            Intent shareIntent = Intent.CreateChooser(intent, (string) null);
            Platform.CurrentActivity.StartActivity(shareIntent);
        }

        public void navigateTelegramChat(int chatID)
        {
            Intent intent = new Intent(Intent.ActionView)
                .SetData(Android.Net.Uri.Parse("http://telegram.me/" + chatID)); //TODO: Check this

            try
            {
                Platform.CurrentActivity.PackageManager.GetPackageInfo("org.telegram.messenger", PackageInfoFlags.Activities);
                intent.SetPackage("org.telegram.messenger");
            }catch(PackageManager.NameNotFoundException e)
            {
                Logger.Warn(e, "Could not go to Telegram as package was not found.");
                return;
            }
            Platform.CurrentActivity.StartActivity(intent);
        }

        public void quitApplication()
        {
            //TODO: Implement this
        }

    }
}