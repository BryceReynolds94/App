﻿using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using PagerBuddy.Services;
using PagerBuddy.Views;
using System.Collections;
using PagerBuddy.ViewModels;
using PagerBuddy.Interfaces;
using System.Threading.Tasks;
using PagerBuddy.Models;
using Xamarin.Essentials;
using System.Collections.ObjectModel;
using PagerBuddy.Resources;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace PagerBuddy
{
    //MEMO: NLog Setup: https://www.jenx.si/2020/07/15/using-nlog-in-xamarin-forms-applications/
    //MEMO: FFImageLoading Setup: https://github.com/luberda-molinet/FFImageLoading/wiki/Xamarin.Forms-API
    //MEMO: FirebasePushNotificationPlugin Setup: https://github.com/CrossGeeks/FirebasePushNotificationPlugin/blob/master/docs/GettingStarted.md
    //MEMO: Generate app icon assets: https://easyappicon.com/ https://www.iconsgenerator.com/


    //TODO Later: Implement rating prompt https://developer.android.com/guide/playcore/in-app-review/ (similar for iOS)

    public partial class App : Application
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private bool isAlert = false;
        private MainPage mainPageMemo;
        public App(bool isAlert = false, Alert alert = null)
        {
            Logger.Debug("Starting App.");

            InitializeComponent();
            VersionTracking.Track(); //Initialise global version tracking

            if (VersionTracking.IsFirstLaunchEver) {
                Logger.Debug("Fresh installation. First run of App with build No. " + VersionTracking.CurrentBuild);
            }else if (VersionTracking.IsFirstLaunchForCurrentBuild) {
                Logger.Debug("App updated from build no. " + VersionTracking.PreviousBuild + ". First run of build no. " + VersionTracking.CurrentBuild);
                UpdaterService.checkUpdate(VersionTracking.PreviousBuild, VersionTracking.CurrentBuild);
            }

            MessagingCenter.Subscribe<AboutPage, Alert>(this, AboutPage.MESSAGING_KEYS.SHOW_ALERT_PAGE.ToString(), (sender, alert) => requestAlertPage(alert));
            MessagingCenter.Subscribe<AlertPage>(this, AlertPage.MESSAGING_KEYS.REQUEST_START_PAGE.ToString(), (sender) => requestStartPage());

            if (isAlert && alert != null) {
                requestAlertPage(alert);
            } else {
                requestStartPage();
            }
        }

        public void requestAlertPage(Alert alert) {
            isAlert = true;

            if(MainPage is MainPage) {
                mainPageMemo = MainPage as MainPage;
            }
            MainPage = new AlertPage(alert);
        }

        public void requestStartPage() {
            isAlert = false;

            if (mainPageMemo != null) {
                MainPage = mainPageMemo;
            } else {
                MainPage = new MainPage();
            }
        }

        protected override async void OnStart(){
            if (!isAlert) {
                //Reload Token on start
                if(Device.RuntimePlatform == Device.Android) {
                    IAndroidNotifications notification = DependencyService.Get<IAndroidNotifications>();
                    notification.RefreshToken();
                }

                CommunicationService client = ((MainPage)Current.MainPage).client;
                await client.connectClient();
                new MessagingService(client);
            }

        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
