using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PagerBuddy.Interfaces;
using Xamarin.Essentials;
using Android.Content.PM;
using Android.Provider;
using PagerBuddy.Resources;
using Xamarin.Forms;
using System.Threading.Tasks;
using Application = Android.App.Application;
using PagerBuddy.Services;
using Android.App.Usage;
using AndroidX.Core.Content;
using Google.Common.Util.Concurrent;

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.Droid.Permissions))] //register for dependency service as platform-specific code
namespace PagerBuddy.Droid {
    class Permissions : IAndroidPermissions {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private async Task permissionNotificationPolicyAccess(Page currentView) {

            bool confirmed = await currentView.DisplayAlert(AppResources.HomeStatusPage_DNDPermissionPrompt_Title,
                AppResources.HomeStatusPage_DNDPermissionPrompt_Message,
                AppResources.HomeStatusPage_DNDPermissionPrompt_Confirm,
                AppResources.HomeStatusPage_DNDPermissionPrompt_Cancel);

            if (confirmed) {
                Intent intent = new Intent(Settings.ActionNotificationPolicyAccessSettings);
                intent.PutExtra(Settings.ExtraAppPackage, Application.Context.PackageName);
                Platform.CurrentActivity.StartActivity(intent);
            }
        }

        private async Task permissionHibernationExclusion(Page currentView) {
            //Check current app setting
            IListenableFuture future = PackageManagerCompat.GetUnusedAppRestrictionsStatus(Application.Context);
            object result = await Task.Run(() => future.Get()); //Make call async to avoid blocking

            switch (result) {
                case UnusedAppRestrictionsConstants.Error:
                    Logger.Warn("Error tyring to retreive unused app restriction status. Attempting request anyway.");
                    break;
                case UnusedAppRestrictionsConstants.Disabled:
                case UnusedAppRestrictionsConstants.FeatureNotAvailable:
                    Logger.Info("Unused app restritcions already disabled or not available. Will not request again.");
                    return;
            }

            bool confirmed = await currentView.DisplayAlert(AppResources.AndroidPermission_HibernationExclude_Title,
               AppResources.AndroidPermission_HibernationExclude_Message,
              AppResources.AndroidPermission_HibernationExclude_Confirm,
              AppResources.AndroidPermission_HibernationExclude_Cancel);

            if (confirmed) {
                //https://github.com/androidx/androidx/blob/androidx-main/core/core/src/main/java/androidx/core/content/IntentCompat.java
                //https://developer.android.com/topic/performance/app-hibernation

                Intent intent = IntentCompat.CreateManageUnusedAppRestrictionsIntent(Application.Context, Application.Context.PackageName);
                try {
                    Platform.CurrentActivity.StartActivityForResult(intent, 0);
                } catch (Exception e) {
                    Logger.Error(e, "Could not launch hibernation exclusion intent.");
                }
            }
        }

        public void logPermissionSettings() {
            NotificationManager manager = NotificationManager.FromContext(Application.Context);
            Logger.Debug("Status of DND policy access: " + manager.IsNotificationPolicyAccessGranted);

            try {
                UsageStatsManager usage = (UsageStatsManager)Application.Context.GetSystemService("usagestats");
                Logger.Debug("App currently in standby bucket: " + usage.AppStandbyBucket);
            } catch (Exception e) {
                Logger.Info(e, "Could not determine app standy bucket.");
            }

            IListenableFuture future = PackageManagerCompat.GetUnusedAppRestrictionsStatus(Application.Context);
            object result = future.Get();
            Logger.Debug("Status of unused app restrictions: " + result);
        }

        public async Task checkAlertPermissions(Page currentView, bool forceReprompt = false) {

            if (!DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DND_PERMISSION, false) || forceReprompt) {
                DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DND_PERMISSION, true);
                await permissionNotificationPolicyAccess(currentView);
            }

            if (!DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_HIBERNATION_EXCLUSION, false) || forceReprompt) {
                DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_HIBERNATION_EXCLUSION, true);
                await permissionHibernationExclusion(currentView);
            }
        }
    }
}