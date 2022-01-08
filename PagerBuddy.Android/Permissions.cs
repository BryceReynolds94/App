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
            bool confirmed = await currentView.DisplayAlert("Exempt from Hibernation", //TODO: RBF - Remove static string
               "To ensure working need no hibernation",
              AppResources.HomeStatusPage_DNDPermissionPrompt_Confirm,
              AppResources.HomeStatusPage_DNDPermissionPrompt_Cancel);

            if (confirmed) {
                //https://developer.android.com/topic/performance/app-hibernation
                //TODO: Implement hibernation exemption

            }
        }

        public void logPermissionSettings() {
            NotificationManager manager = NotificationManager.FromContext(Application.Context);
            Logger.Debug("Status of DND policy access: " + manager.IsNotificationPolicyAccessGranted);

            UsageStatsManager usage = (UsageStatsManager) Application.Context.GetSystemService("usagestats");
            Logger.Debug("App currently in standby bucket: " + usage.AppStandbyBucket);
        }

        public async Task checkAlertPermissions(Page currentView, bool forceReprompt = false) {

            if (!DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DND_PERMISSION, false) || forceReprompt) {
                await permissionNotificationPolicyAccess(currentView);
                DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DND_PERMISSION, true);
            }
        }
    }
}