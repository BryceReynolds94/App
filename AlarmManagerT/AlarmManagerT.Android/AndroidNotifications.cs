using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlarmManagerT.Droid;
using AlarmManagerT.Models;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;


[assembly: Xamarin.Forms.Dependency(typeof(AndroidNotifications))] //´register for dependency service as platform-specific code
namespace AlarmManagerT.Droid
{
    public class AndroidNotifications : INotifications
    {
        public void showNotification(string title){
            NotificationCompat.Builder builder = new NotificationCompat.Builder(Application.Context, "CHANNEL_ALERT")
                .SetContentTitle(title)
                .SetSmallIcon(Resource.Drawable.xamarin_logo)
                .SetContentText("sample Text")
                .SetPriority(NotificationCompat.PriorityHigh);

            NotificationManagerCompat manager = NotificationManagerCompat.From(Application.Context);
            manager.Notify(123456, builder.Build());
                
            }

    }
}