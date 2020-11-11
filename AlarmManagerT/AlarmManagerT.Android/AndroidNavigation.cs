using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlarmManagerT.Models;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Provider;
using Xamarin.Essentials;

[assembly: Xamarin.Forms.Dependency(typeof(AlarmManagerT.Droid.AndroidNavigation))] //register for dependency service as platform-specific code
namespace AlarmManagerT.Droid
{
    class AndroidNavigation : INavigation
    {
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

    }
}