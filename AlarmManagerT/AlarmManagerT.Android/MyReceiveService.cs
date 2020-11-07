using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Firebase.Messaging;

namespace AlarmManagerT.Droid
{
    [Service]
    [IntentFilter(new String[] { "com.google.firebase.MESSAGING_EVENT" })]
    class MyReceiveService : FirebaseMessagingService
    {
        public override void OnMessageReceived(RemoteMessage msg)
        {
            base.OnMessageReceived(msg);
            RemoteMessage.Notification notif = msg.GetNotification(); // null for Telegram FCM -> init check Telegram
            AndroidNotifications notifications = new AndroidNotifications();
            notifications.showAlertNotification("Notification Alive", "Urgent stuff");
        }
    }
}