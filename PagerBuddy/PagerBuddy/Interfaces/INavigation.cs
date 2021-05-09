using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PagerBuddy.Interfaces
{
    public interface INavigation
    {
        void navigateNotificationSettings();
        void navigateNotificationPolicyAccess();
        void navigateDozeExempt();
        void navigateHuaweiPowerException();
        void navigateTelegramChat(int chatID, TelegramPeer.TYPE type);
        void quitApplication();
        bool isTelegramInstalled();

        void logPermissionSettings();
    }
}
