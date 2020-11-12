using AlarmManagerT.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlarmManagerT.Interfaces
{
    public interface INotifications
    {
        void showAlertNotification(Alert alert);

        void showStandardNotification(string title, string text);

    }
}
