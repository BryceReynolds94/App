using System;
using System.Collections.Generic;
using System.Text;

namespace AlarmManagerT.Interfaces
{
    public interface INavigation
    {
        void navigateNotificationSettings();
        void navigateNotificationPolicyAccess();
        void navigateTelegramChat(int chatID);
        void quitApplication();
    }
}
