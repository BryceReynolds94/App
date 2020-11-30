using System;
using System.Collections.Generic;
using System.Text;

namespace PagerBuddy.Interfaces
{
    public interface INavigation
    {
        void navigateNotificationSettings();
        void navigateNotificationPolicyAccess();
        void navigateTelegramChat(int chatID);
        void navigateShareFile(string fileName);
        void quitApplication();
    }
}
