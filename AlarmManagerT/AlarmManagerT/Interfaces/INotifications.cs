using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PagerBuddy.Interfaces
{
    public interface INotifications
    {
        void showAlertNotification(Alert alert);

        void showStandardNotification(string title, string text);

        void addNotificationChannel(AlertConfig config);
        void removeNotificationChannel(AlertConfig config);

    }
}
