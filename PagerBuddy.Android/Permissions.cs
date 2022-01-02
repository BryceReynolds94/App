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

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.Droid.Permissions))] //register for dependency service as platform-specific code
namespace PagerBuddy.Droid {
    class Permissions : IAndroidPermissions {

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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

        private void permissionDozeExempt() {
            //https://developer.android.com/training/monitoring-device-state/doze-standby#exemption-cases
            Intent intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
            intent.SetData(Android.Net.Uri.Parse("package:" + Application.Context.PackageName));
            Platform.CurrentActivity.StartActivity(intent);
        }

        private async Task permissionHuaweiPowerException(Page currentView) {
            bool confirmed = await currentView.DisplayAlert(AppResources.HomeStatusPage_HuaweiPrompt_Title,
                    AppResources.HomeStatusPage_HuaweiPrompt_Message,
                    AppResources.HomeStatusPage_HuaweiPrompt_Confirm,
                    AppResources.HomeStatusPage_HuaweiPrompt_Cancel);

            if (!confirmed) {
                return;
            }

            Intent Huawei1 = new Intent().SetComponent(new ComponentName("com.huawei.systemmanager", "com.huawei.systemmanager.startupmgr.ui.StartupNormalAppListActivity"));
            Intent Huawei2 = new Intent().SetComponent(new ComponentName("com.huawei.systemmanager", "com.huawei.systemmanager.optimize.process.ProtectActivity"));
            Intent Huawei3 = new Intent().SetComponent(new ComponentName("com.huawei.systemmanager", "com.huawei.systemmanager.appcontrol.activity.StartupAppControlActivity"));

            try {
                if (Application.Context.PackageManager.ResolveActivity(Huawei1, PackageInfoFlags.MatchDefaultOnly) != null) {
                    Logger.Debug("Starting HuaweiPowerException-Activity 1.");
                    Platform.CurrentActivity.StartActivity(Huawei1);
                    return;
                }
                if (Application.Context.PackageManager.ResolveActivity(Huawei2, PackageInfoFlags.MatchDefaultOnly) != null) {
                    Logger.Debug("Starting HuaweiPowerException-Activity 2.");
                    Platform.CurrentActivity.StartActivity(Huawei2);
                    return;
                }
                if (Application.Context.PackageManager.ResolveActivity(Huawei3, PackageInfoFlags.MatchDefaultOnly) != null) {
                    Logger.Debug("Starting HuaweiPowerException-Activity 3.");
                    Platform.CurrentActivity.StartActivity(Huawei3);
                    return;
                }
            } catch (Exception e) {
                Logger.Error(e, "Exception trying to open Huawei power exception activities.");
            }

        }

        public void logPermissionSettings() {
            NotificationManager manager = NotificationManager.FromContext(Application.Context);
            Logger.Debug("Status of DND policy access: " + manager.IsNotificationPolicyAccessGranted);

            PowerManager powerManager = PowerManager.FromContext(Application.Context);
            Logger.Debug("Status of doze exemption: " + powerManager.IsIgnoringBatteryOptimizations(Application.Context.PackageName));

        }

        public async Task checkAlertPermissions(Page currentView, bool forceReprompt = false) {
            if (!DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DOZE_EXEMPT, false) || forceReprompt) {
                permissionDozeExempt();
                DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DOZE_EXEMPT, true);
            }

            if (!DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DND_PERMISSION, false) || forceReprompt) {
                await permissionNotificationPolicyAccess(currentView);
                DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DND_PERMISSION, true);
            }

            if (DeviceInfo.Manufacturer.Contains("HUAWEI", StringComparison.OrdinalIgnoreCase) && !DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_HUAWEI_EXEPTION, false) || forceReprompt) {
                await permissionHuaweiPowerException(currentView);
                DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_HUAWEI_EXEPTION, true);
            }
        }
    }
}