﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Java.Interop;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//TODO: Later - Implement animated theme https://medium.com/swlh/splash-screen-in-android-8ab250e40190

namespace PagerBuddy.Droid {
    [Activity(Label = "@string/app_name", Icon = "@mipmap/ic_launcher", Theme = "@style/SplashTheme", MainLauncher = true, NoHistory = true)]
    public class SplashScreenActivity : AppCompatActivity {
        public override void OnCreate(Bundle savedInstanceState, PersistableBundle persistentState) {
            base.OnCreate(savedInstanceState, persistentState);

        }

        protected override void OnResume() {
            base.OnResume();
            StartActivity(new Intent(Application.Context, typeof(MainActivity)));
        }

    }

}