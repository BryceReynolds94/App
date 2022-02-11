using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BackgroundTasks;
using FFImageLoading.Forms.Platform;
using FFImageLoading.Svg.Forms;
using Firebase.CloudMessaging;
using Foundation;
using PagerBuddy.Models;
using PagerBuddy.Services;
using UIKit;
using UserNotifications;

namespace PagerBuddy.iOS {
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate {
        private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //

        private PagerBuddy.App XFApp;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options) {
            global::Xamarin.Forms.Forms.Init();

            CachedImageRenderer.Init(); //Added to enable FFImageLoading
            CachedImageRenderer.InitImageSourceHandler();

            var ignore = typeof(SvgCachedImage); //Added to enable SVG FFImageLoading

            Firebase.Core.App.Configure();
            Messaging.SharedInstance.Delegate = new MessagingDelegate();

            //https://developer.apple.com/documentation/uikit/app_and_environment/scenes/preparing_your_ui_to_run_in_the_background/using_background_tasks_to_update_your_app
            bool res = BGTaskScheduler.Shared.Register(ServerRequestScheduler.SERVER_REFRESH_TASK, null, new Action<BGTask>(async (BGTask task) => {
                if (ServerRequestScheduler.instance != null) {
                    await ServerRequestScheduler.instance.backgroundRequest(task);
                } else {
                    await new ServerRequestScheduler().backgroundRequest(task);
                }
            }));

            ApplyStyle();

            XFApp = new App(false, null);
            LoadApplication(XFApp);

            UNUserNotificationCenter.Current.Delegate = new UserNotificationCenterDelegate();
            UIApplication.SharedApplication.RegisterForRemoteNotifications();

            return base.FinishedLaunching(app, options);
        }

        private void ApplyStyle() {
            UIColor accent = new UIColor(red: new nfloat(0.35), green: new nfloat(0.45), blue: new nfloat(0.79), alpha: new nfloat(1));
            //UIColor primary = new UIColor(red: new nfloat(0.11), green: new nfloat(0.29), blue: new nfloat(0.60), alpha: new nfloat(1));

            UISwitch.Appearance.OnTintColor = accent;

        }


        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) {
            Messaging.SharedInstance.ApnsToken = deviceToken;

            if (!Xamarin.Forms.Forms.IsInitialized) //Not sure if this ever happens
            {
                Logger.Debug("Token update received in background. Initialising Platform.");
                Xamarin.Forms.Forms.Init(); //We need to make sure Xamarin.Forms is initialised when notifications are received in killed state  
            }
            MessagingService.TokenRefresh(Messaging.SharedInstance.FcmToken);
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error) {
            if (error.Code == 3010) {
                Logger.Warn("Remote notifications not available in simulator.");
            } else {
                Logger.Error(error.Description, "Could not register for remote notifications. This is probably fatal.");
            }
        }

        // To receive notifications in background in any iOS version
        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler) {
            //This is called when opening a notification after received (after app is started)

            if (Xamarin.Forms.Forms.IsInitialized){
                Logger.Debug("Received remote notification.");
                Alert alert = extractAlertData(userInfo); //May be null
                if (alert != null) {
                    AlertService.logAlert(alert);
                    XFApp?.requestAlertPage(alert);
                }
                completionHandler(UIBackgroundFetchResult.NewData);
            } else {
                completionHandler(UIBackgroundFetchResult.NoData);
            }
        }

        private Alert extractAlertData(NSDictionary data) {
            if(data == null) {
                return null;
            }

            bool res = data.TryGetValue(new NSString("zvei_description"), out NSObject descriptionR);
            //res &= data.TryGetValue(new NSString("zvei"), out NSObject zveiR);
            res &= data.TryGetValue(new NSString("is_test_alert"), out NSObject testAlertR);
            res &= data.TryGetValue(new NSString("is_manual_test_alert"), out NSObject manualAlertR);
            res &= data.TryGetValue(new NSString("alert_timestamp"), out NSObject alertTimestampR);
            res &= data.TryGetValue(new NSString("chat_id"), out NSObject chatIDR);

            if (!res) {
                Logger.Error("Error parsing remote notification data. Ignoring it.");
                return null;
            }

            string alertTimestampS = alertTimestampR.ToString();
            string testAlertS = testAlertR.ToString();
            string chatIDS = chatIDR.ToString();
            string manualAlertS = manualAlertR.ToString();

            if(!bool.TryParse(manualAlertS, out bool manualAlert)) {
                Logger.Warn("Could not parse manual alert from data. Using default value. String value was: " + manualAlertS);
                manualAlert = false;
            }
            if (!bool.TryParse(testAlertS, out bool testAlert)) {
                Logger.Warn("Could not parse test alert from data. Using default value. String value was: " + testAlertS);
                testAlert = false;
            }
            if (!long.TryParse(alertTimestampS, out long alertTimestamp) || !long.TryParse(chatIDS, out long chatID)) {
                Logger.Error("Could not parse alert data. Ignoring message. Payload: " + data.ToString());
                return null;
            }

            DateTime alertTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(alertTimestamp).ToLocalTime();
            AlertConfig config = getConfigFromChatID(chatID, manualAlert);

            if (config != null) {
                return new Alert(descriptionR.ToString(), alertTime, testAlert, config);
            }
            return null;
        }

        private AlertConfig getConfigFromChatID(long chatID, bool isManual) {
            Collection<string> configList = DataService.getConfigList();
            foreach (string configID in configList) {
                AlertConfig config = DataService.getAlertConfig(configID, null);
                if (config != null && (config.triggerGroup.serverID == chatID || isManual)) {
                    return config;
                }
            }
            return null;
        }

    }
}
