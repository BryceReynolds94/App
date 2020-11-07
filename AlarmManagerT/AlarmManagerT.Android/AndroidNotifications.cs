using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlarmManagerT.Models;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Android.Support.V4.App; //needed for compat notifications https://docs.microsoft.com/en-us/xamarin/android/app-fundamentals/notifications/local-notifications-walkthrough


[assembly: Xamarin.Forms.Dependency(typeof(AlarmManagerT.Droid.AndroidNotifications))] //register for dependency service as platform-specific code
namespace AlarmManagerT.Droid
{
    public class AndroidNotifications : INotifications
    {
        public static readonly string ALERT_CHANNEL_ID = "Alert Notifications";
        public static readonly string STANDARD_CHANNEL_ID = "Standard Notifications";


        public void showAlertNotification(string title, string text){ //TODO: Include Logo for message
            NotificationCompat.Builder builder = new NotificationCompat.Builder(Application.Context, ALERT_CHANNEL_ID)
                .SetContentTitle(title)
                .SetSmallIcon(Resource.Drawable.xamarin_logo) //TODO: Adjust Logo
                .SetContentText(text)
                .SetPriority(NotificationCompat.PriorityHigh)
                .SetCategory(NotificationCompat.CategoryCall); //TODO: Decide if categoryCall or Alarm 

            //TODO: Add full-screen intent https://developer.android.com/training/notify-user/time-sensitive

            NotificationManagerCompat manager = NotificationManagerCompat.From(Application.Context);
            manager.Notify(new Random().Next(), builder.Build()); //Currently no need to access notification later - so set ID random and forget
                
            }

        public void showStandardNotification(string title, string text)
        {
            NotificationCompat.Builder builder = new NotificationCompat.Builder(Application.Context, STANDARD_CHANNEL_ID)
                .SetContentTitle(title)
                .SetSmallIcon(Resource.Drawable.xamarin_logo) //TODO: Adjust Logo
                .SetContentText(text)
                .SetPriority(NotificationCompat.PriorityDefault); //TODO: Decide if adequate category

            NotificationManagerCompat manager = NotificationManagerCompat.From(Application.Context);
            manager.Notify(new Random().Next(), builder.Build()); //Possibly change to access/clear notification by ID
        }



    }

}