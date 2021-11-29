using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace PagerBuddy.Interfaces
{
    public interface INotification {
        void showStandardNotification(string title, string text);
    }

    public interface IAndroidNotification : INotification {
        void showAlertNotification(Alert alert);
        void closeNotification(int notificationID);
        void addNotificationChannel(AlertConfig config); //Android
        void removeNotificationChannel(AlertConfig config); //Android
        void UpdateNotificationChannels(Collection<AlertConfig> configList); //Android
    }

    public interface IiOSNotification : INotification {
    }
}
