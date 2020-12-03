﻿using System;
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

        public void navigateShareFile(string fileName) {

            Android.Net.Uri contentUri = FileProvider.GetUriForFile(Application.Context, "de.bartunik.pagerbuddy.fileprovider", new Java.IO.File(fileName));
            //FileProvider defined in Manifest

            Intent intent = new Intent(Intent.ActionSend)
                .SetData(contentUri)
                .PutExtra(Intent.ExtraStream, contentUri)
                .PutExtra(Intent.ExtraEmail, Resources.AppResources.App_DeveloperContact)
                .AddFlags(ActivityFlags.GrantReadUriPermission)
                .SetType("text/plain");
            Platform.CurrentActivity.StartActivity(Intent.CreateChooser(intent, (string) null));
        }

        public void navigateTelegramChat(int chatID)
        {
            if (!isTelegramInstalled()) {
                quitApplication();
                return;
            }
            Platform.CurrentActivity.StartActivity(getTelegramIntent(chatID));
        }

        public static Intent getTelegramIntent(int chatID) {
            Intent intent = new Intent(Intent.ActionView)
                .SetData(Android.Net.Uri.Parse("http://telegram.me/" + chatID)) //TODO: Find correct intent syntax
                .SetPackage("org.telegram.messenger");
            return intent;
        }

        public static bool isTelegramInstalled() {
            try {
                Platform.CurrentActivity.PackageManager.GetPackageInfo("org.telegram.messenger", PackageInfoFlags.Activities);
            } catch (PackageManager.NameNotFoundException e) {
                Logger.Warn(e, "Telegram package is not installed.");
                return false;
            }
            return true;
        }

        public void quitApplication()
        {
            Intent intent = new Intent(Intent.ActionMain)
                .AddCategory(Intent.CategoryHome)
                .AddFlags(ActivityFlags.NewTask);
            Platform.CurrentActivity.StartActivity(intent);
        }

    }
}