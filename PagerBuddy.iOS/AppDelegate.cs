using System;
using System.Collections.Generic;
using System.Linq;
using FFImageLoading.Forms.Platform;
using FFImageLoading.Svg.Forms;
using Foundation;
using PagerBuddy.Services;
using UIKit;

namespace PagerBuddy.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
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

            LoadApplication(new App());
            //FirebasePushNotificationManager.Initialize(options, true); //Init FirebasePushNotification Plugin

            return base.FinishedLaunching(app, options);
        }

        //Callbacks for FirebasePushNotification Plugin
        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) {
            //FirebasePushNotificationManager.DidRegisterRemoteNotifications(deviceToken);
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error) {
            //FirebasePushNotificationManager.RemoteNotificationRegistrationFailed(error);

        }
        // To receive notifications in foregroung on iOS 9 and below.
        // To receive notifications in background in any iOS version
        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler) {
            // If you are receiving a notification message while your app is in the background,
            // this callback will not be fired 'till the user taps on the notification launching the application.

            // If you disable method swizzling, you'll need to call this method. 
            // This lets FCM track message delivery and analytics, which is performed
            // automatically with method swizzling enabled.
            //FirebasePushNotificationManager.DidReceiveMessage(userInfo);
            // Do your magic to handle the notification data

            if (!Xamarin.Forms.Forms.IsInitialized) //TODO: iOS Testing
                {
                Xamarin.Forms.Forms.Init(); //We need to make sure Xamarin.Forms is initialised when notifications are received in killed state
                MessagingService.FirebaseMessage(null, DateTime.Now);
            }

            completionHandler(UIBackgroundFetchResult.NewData);
        }

    }
}
