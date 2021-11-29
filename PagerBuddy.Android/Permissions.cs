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

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.Droid.Permissions))] //register for dependency service as platform-specific code
namespace PagerBuddy.Droid {
    class Permissions : IAndroidPermissions {

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public void permissionNotificationPolicyAccess() {
            Intent intent = new Intent(Settings.ActionNotificationPolicyAccessSettings);
            intent.PutExtra(Settings.ExtraAppPackage, Application.Context.PackageName);
            Platform.CurrentActivity.StartActivity(intent);
        }

        public void permissionDozeExempt() {
            Intent intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
            intent.SetData(Android.Net.Uri.Parse("package:" + Application.Context.PackageName));
            Platform.CurrentActivity.StartActivity(intent);
        }

        public void permissionHuaweiPowerException() {
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
    }
}