using System;
using System.Collections.Generic;
using System.Text;

namespace AlarmManagerT.Models
{
    public interface INotifications
    {
        void showAlertNotification(string title, string text);

        void showStandardNotification(string title, string text);

    }
}
