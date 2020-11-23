using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using Plugin.FirebasePushNotification;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace PagerBuddy.Services
{
    public class MessagingService
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public void SetupListeners(CommunicationService client)
        {
            //subscribe to token changes
            CrossFirebasePushNotification.Current.OnTokenRefresh += async (s, args) =>
                {
                    Logger.Info("Firebase token was updated, TOKEN: {0}", args.Token);

                    if (client.clientStatus == CommunicationService.STATUS.AUTHORISED) {
                        await client.subscribePushNotifications(args.Token);
                    }
                };

            //foreground notification listener
            CrossFirebasePushNotification.Current.OnNotificationReceived += (s, p) =>
                {
                    Logger.Debug("A firebase message was received while app is active.");
                    new AlertService(client);
                };
        }


        //This is called when FCM Messages are received after app is killed
        public static void BackgroundFirebaseMessage(object sender, FirebasePushNotificationDataEventArgs args)
        {
            Logger.Debug("A firebase message was received while app is in background.");
            new AlertService();
        }

    }
}
