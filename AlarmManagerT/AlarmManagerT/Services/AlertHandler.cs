using AlarmManagerT.Models;
using Plugin.FirebasePushNotification;
using Plugin.LocalNotification;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace AlarmManagerT.Services
{
    public class AlertHandler
    {
        public AlertHandler()
        {
            //TODO: Replace & Remove FCM samples
            CrossFirebasePushNotification.Current.OnNotificationReceived += (s, p) =>
            {
                IDictionary keyValuePairs = (IDictionary)p.Data;
                onNotification(s, p);

                System.Diagnostics.Debug.WriteLine("Received");

            };

            CrossFirebasePushNotification.Current.OnNotificationOpened += (s, p) =>
            {
                //onNotification(s, p);


                //TODO: Remove sample code
                System.Diagnostics.Debug.WriteLine("Opened");
                foreach (var data in p.Data)
                {
                    System.Diagnostics.Debug.WriteLine($"{data.Key} : {data.Value}");
                }


            };

            CrossFirebasePushNotification.Current.OnNotificationAction += (s, p) =>
            {
                //onNotification(s, p);

                //TODO: Remove sample code
                System.Diagnostics.Debug.WriteLine("Action");

                if (!string.IsNullOrEmpty(p.Identifier))
                {
                    System.Diagnostics.Debug.WriteLine($"ActionId: {p.Identifier}");
                    foreach (var data in p.Data)
                    {
                        System.Diagnostics.Debug.WriteLine($"{data.Key} : {data.Value}");
                    }

                }

            };

            MessagingCenter.Subscribe(this, "FirebaseNotificationReceived", (object sender, FirebasePushNotificationDataEventArgs args) => onNotification(sender, args));
        }

        private void onNotification(object sender, FirebasePushNotificationDataEventArgs args)
        {
            //TODO: Handle Notifications
            showUserNotification("We are here");
            return;
        }

        public void showUserNotification(string text)
        {
            try
            {
                INotifications notificationInterface = DependencyService.Get<INotifications>();
                notificationInterface.showNotification(text);
            }catch(Exception e) {
                return;
            }
        }

    }
}
