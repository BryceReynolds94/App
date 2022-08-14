using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace PagerBuddy.Interfaces
{
    public interface INotifications {
        void showAlertNotification(Alert alert, int percentVolume);
        void showToast(string message);
    }
    public interface IiOSNotifications : INotifications {

    }
    public interface IAndroidNotifications : INotifications {
        void showStandardNotification(string title, string text);
        void closeNotification(int notificationID);
        void addNotificationChannel(AlertConfig config);
        void removeNotificationChannel(AlertConfig config);
        void UpdateNotificationChannels(Collection<AlertConfig> configList);
        Action playChannelRingtone(string alertConfigID);
        void RefreshToken();

        void rotateIDs();
    }
}
