using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Essentials;

namespace PagerBuddy.Droid {

    [BroadcastReceiver(Enabled =true, Exported = false)]
    [IntentFilter(new[] { Intent.ActionMyPackageReplaced})]
    class UpdateBroadcastReceiver : BroadcastReceiver {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public override void OnReceive(Context context, Intent intent) {

            if(UpdaterService.checkNotification(VersionTracking.PreviousBuild, VersionTracking.CurrentBuild)) {
                Logger.Info("Breaking package update detected. Showing notification.");
                new Notifications().showStandardNotification(Resources.AppResources.Notification_BreakingUpdate_Title, Resources.AppResources.Notification_BreakingUpdate_Message);
            }
        }
    }
}