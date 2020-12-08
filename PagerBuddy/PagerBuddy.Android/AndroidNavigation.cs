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
using Android.Provider;
using Xamarin.Essentials;
using Android.Content.PM;
using System.IO;

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.Droid.AndroidNavigation))] //register for dependency service as platform-specific code
namespace PagerBuddy.Droid
{
    class AndroidNavigation : INavigation
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public void navigateNotificationSettings()
        {
            Intent intent = new Intent(Settings.ActionAppNotificationSettings);
            intent.PutExtra(Settings.ExtraAppPackage, Application.Context.PackageName); 
            Platform.CurrentActivity.StartActivity(intent);
        }

        public void navigateNotificationPolicyAccess()
        {
            Intent intent = new Intent(Settings.ActionNotificationPolicyAccessSettings);
            intent.PutExtra(Settings.ExtraAppPackage, Application.Context.PackageName);
            Platform.CurrentActivity.StartActivity(intent);
        }

        public void navigateTelegramChat(int chatID)
        {
            if (!isTelegramInstalled()) {
                quitApplication();
                return;
            }
            Application.Context.StartActivity(getTelegramIntent(chatID));
        }

        //Telegram intent handling:
        //https://github.com/DrKLO/Telegram/blob/5a47056c7b1cb0b7d095ca6e7d1a288c01f8f160/TMessagesProj/src/main/java/org/telegram/ui/LaunchActivity.java#L1101
        public static Intent getTelegramIntent(int chatID) {
            Intent intent = new Intent(Intent.ActionView)
                .SetData(Android.Net.Uri.Parse("tg:openmessage/?chat_id=" + chatID))
                .SetPackage("org.telegram.messenger")
                .AddFlags(ActivityFlags.NewTask);
            return intent;
        }

        public bool isTelegramInstalled() {
            try {
                Application.Context.PackageManager.GetPackageInfo("org.telegram.messenger", PackageInfoFlags.Activities); 
            } catch (PackageManager.NameNotFoundException) {
                Logger.Warn("Telegram package is not installed.");
                return false;
            }
            return true;
        }

        public void quitApplication()
        {
            Intent intent = new Intent(Intent.ActionMain)
                .AddCategory(Intent.CategoryHome)
                .AddFlags(ActivityFlags.NewTask);
            Application.Context.StartActivity(intent);
        }

    }
}