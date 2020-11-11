using AlarmManagerT.Interfaces;
using Plugin.FirebasePushNotification;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace AlarmManagerT.Services
{
    public class FirebaseMessagingHandler
    {

        public void SetupListeners(MyClient client)
        {

            //subscribe to token changes
            CrossFirebasePushNotification.Current.OnTokenRefresh += async (s, args) =>
                {
                    //TODO: Replace logging
                    System.Diagnostics.Debug.WriteLine($"TOKEN : {args.Token}");
                    await client.subscribePushNotifications(args.Token);
                };


            //Foreground notification listener
            CrossFirebasePushNotification.Current.OnNotificationReceived += (s, p) =>
                {

                    AlertHandler handler = new AlertHandler();
                    handler.checkForNewMessages(client);

                    INotifications notifications = DependencyService.Get<INotifications>();
                    notifications.showAlertNotification("TESTPOINT Notification", "154684625"); //TODO: RBF
                };


        }


        //This is called whe FCM Messages are received after app is killed
        public static void BackgroundFirebaseMessage(object sender, FirebasePushNotificationDataEventArgs args)
        {
            AlertHandler handler = new AlertHandler();
            handler.checkForNewMessages();

            INotifications notifications = DependencyService.Get<INotifications>();
            notifications.showAlertNotification("TESTPOINT Notification", "23456448456432"); //TODO: RBF
        }

    }
}
