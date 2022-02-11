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
            bool confirmed = await currentView.DisplayAlert(AppResources.AndroidPermission_HibernationExclude_Title,
               AppResources.AndroidPermission_HibernationExclude_Message,
              AppResources.AndroidPermission_HibernationExclude_Confirm,
              AppResources.AndroidPermission_HibernationExclude_Cancel);

            if (confirmed) {
                //https://github.com/androidx/androidx/blob/androidx-main/core/core/src/main/java/androidx/core/content/IntentCompat.java

                //https://developer.android.com/topic/performance/app-hibernation
                //TODO: Later - Replace manual implementation with AndroidX

                string intentString;
                if(Build.VERSION.SdkInt > BuildVersionCodes.R){
                    intentString = Settings.ActionApplicationDetailsSettings;
                } else{

                    intentString = Intent.ActionAutoRevokePermissions;
                }
                Intent intent = new Intent(intentString).SetData(Android.Net.Uri.FromParts("package", Application.Context.PackageName, null));
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
            }catch(Exception e) {
                Logger.Info(e, "Could not determine app standy bucket.");
            }

            
            //TODO: Later - Add logging for hibernation exemption
        }

        public async Task checkAlertPermissions(Page currentView, bool forceReprompt = false) {

            if (!DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DND_PERMISSION, false) || forceReprompt) {
                await permissionNotificationPolicyAccess(currentView);
                DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DND_PERMISSION, true);
            }
            //TODO: Later - Implement for API <30 once AndroidX.Core v1.7.0 is available
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R) {
                if (!DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_HIBERNATION_EXCLUSION, false) || forceReprompt) {
                    await permissionHibernationExclusion(currentView);
                    DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_HIBERNATION_EXCLUSION, true);
                }
            }
        }
    }
}