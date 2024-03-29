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
using PagerBuddy.Models;

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.Droid.Navigation))] //register for dependency service as platform-specific code
namespace PagerBuddy.Droid
{
    class Navigation : IAndroidNavigation
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public void navigateNotificationSettings()
        {
            Intent intent = new Intent(Settings.ActionAppNotificationSettings);
            intent.PutExtra(Settings.ExtraAppPackage, Application.Context.PackageName); 
            Platform.CurrentActivity.StartActivity(intent);
        }

     

        public void navigateTelegramChat(long chatID, TelegramPeer.TYPE type)
        {
            if (isTelegramInstalled()) {
                Application.Context.StartActivity(getTelegramIntent(chatID, type));
            }
        }

        //Telegram intent handling:
        //https://github.com/DrKLO/Telegram/blob/5a47056c7b1cb0b7d095ca6e7d1a288c01f8f160/TMessagesProj/src/main/java/org/telegram/ui/LaunchActivity.java#L1101
        public static Intent getTelegramIntent(long peerID, TelegramPeer.TYPE type) {
            string requestURL = "tg:openmessage/?chat_id=";
            if(type == TelegramPeer.TYPE.USER) {
                requestURL = "tg:openmessage/?user_id=";
            }

            Intent intent = new Intent(Intent.ActionView)
                .SetData(Android.Net.Uri.Parse(requestURL + peerID))
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
            Platform.CurrentActivity.FinishAndRemoveTask(); 
        }

    }
}