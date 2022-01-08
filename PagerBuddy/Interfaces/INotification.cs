using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace PagerBuddy.Interfaces
{
    public interface IAndroidNotification {
        void showStandardNotification(string title, string text);
        void showAlertNotification(Alert alert);
        void closeNotification(int notificationID);
        void addNotificationChannel(AlertConfig config); //Android
        void removeNotificationChannel(AlertConfig config); //Android
        void UpdateNotificationChannels(Collection<AlertConfig> configList); //Android
        Action playChannelRingtone(string alertConfigID);

        void RefreshToken();
    }
}
