using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using FFImageLoading.Forms.Platform;
using FFImageLoading.Svg.Forms;
using Firebase.Messaging;
using System.Drawing;
using Android.Content;
using System.Collections.Generic;
using PagerBuddy.Models;
using PagerBuddy.Services;
using PagerBuddy.Resources;
using Android.Gms.Common;
using AndroidX.AppCompat.App;

namespace PagerBuddy.Droid
{
    [Activity(Label = "@string/app_name", Icon = "@mipmap/ic_launcher", Theme = "@style/MainTheme", MainLauncher = false, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo; //Disable dark mode --Possibly move above base call

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            CachedImageRenderer.Init(true); //Addded to enable FFImageLoading
            CachedImageRenderer.InitImageViewHandler();

#pragma warning disable IDE0059 // Unnecessary assignment of a value
            var ignore = typeof(SvgCachedImage); //Added to enable SVG FFImageLoading
#pragma warning restore IDE0059 // Unnecessary assignment of a value

            WakeScreenAlertPage(Intent.HasExtra(Alert.EXTRAS.ALERT_FLAG.ToString()));
            LoadApplication(new App(Intent.HasExtra(Alert.EXTRAS.ALERT_FLAG.ToString()), GetAlertFromIntent(Intent)));
            CheckPlayServices(); //Try our best to inform user of missing Play Services
            new Notifications().SetupNotificationChannels(); //Application has to be loaded first
        }

        private void WakeScreenAlertPage(bool isAlert) {
            if (isAlert) {
                SetShowWhenLocked(true);
                SetTurnScreenOn(true);
                ((KeyguardManager)GetSystemService(Context.KeyguardService)).RequestDismissKeyguard(this, null);
            }
        }
         
        private Alert GetAlertFromIntent(Intent intent)
        {
            if (intent.HasExtra(Alert.EXTRAS.ALERT_FLAG.ToString())){
                Alert alert =  DataService.deserialiseObject<Alert>(intent.GetStringExtra(Alert.EXTRAS.ALERT_FLAG.ToString()));
                return alert;
            }
            return null;
        }

        private void CheckPlayServices() {
            GoogleApiAvailability api = GoogleApiAvailability.Instance;
            int status = api.IsGooglePlayServicesAvailable(this);
            if(status != ConnectionResult.Success) {
                Logger.Error("Google Play Services are not available. The App will not function properly.");
                if (api.IsUserResolvableError(status)) {
                    api.GetErrorDialog(this, status, 1).Show();
                } else {
                    Logger.Error("Missing Google Play Services not resolvable by user. Critical failure. Status: " + status);
                }
            }

        }


    }
}