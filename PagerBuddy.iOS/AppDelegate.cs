using System;
using System.Collections.Generic;
using System.Linq;
using BackgroundTasks;
using FFImageLoading.Forms.Platform;
using FFImageLoading.Svg.Forms;
using Firebase.CloudMessaging;
using Foundation;
using PagerBuddy.Services;
using UIKit;
using UserNotifications;

namespace PagerBuddy.iOS
{
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
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();

            CachedImageRenderer.Init(); //Added to enable FFImageLoading
            CachedImageRenderer.InitImageSourceHandler();

            var ignore = typeof(SvgCachedImage); //Added to enable SVG FFImageLoading

            Firebase.Core.App.Configure();

            //https://developer.apple.com/documentation/uikit/app_and_environment/scenes/preparing_your_ui_to_run_in_the_background/using_background_tasks_to_update_your_app
            bool res = BGTaskScheduler.Shared.Register(ServerRequestScheduler.SERVER_REFRESH_TASK, null, new Action<BGTask>(async (BGTask task) => {
                if (ServerRequestScheduler.instance != null) {
                    await ServerRequestScheduler.instance.backgroundRequest(task);
                } else {
                    await new ServerRequestScheduler().backgroundRequest(task);
                }
            }));

            ApplyStyle();

            LoadApplication(new App());

            UNUserNotificationCenter.Current.Delegate = new UserNotificationCenterDelegate();
            Messaging.SharedInstance.Delegate = new MessagingDelegate();

            UIApplication.SharedApplication.RegisterForRemoteNotifications();

            return base.FinishedLaunching(app, options);
        }

        private void ApplyStyle() {
            UIColor accent = new UIColor(red: new nfloat(0.35), green: new nfloat(0.45), blue: new nfloat(0.79), alpha: new nfloat(1));
            UIColor primary = new UIColor(red: new nfloat(0.11), green: new nfloat(0.29), blue: new nfloat(0.60), alpha: new nfloat(1));

            UISwitch.Appearance.OnTintColor = accent;

        }


        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) {
            Logger.Debug("Registered for remote notification. Device token: " + deviceToken);
            //Do we need this? We should be receiving this in MessagingDelegate
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
            // If you are receiving a notification message while your app is in the background,
            // this callback will not be fired 'till the user taps on the notification launching the application.

            // If you disable method swizzling, you'll need to call this method. 
            // This lets FCM track message delivery and analytics, which is performed
            // automatically with method swizzling enabled.

            Logger.Info("Received remote notification.");
            completionHandler(UIBackgroundFetchResult.NewData);

            //Do we need this? Should be handeled in UserNotificationCenterDelegate
        }

    }
}
