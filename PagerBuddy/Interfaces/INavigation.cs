using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PagerBuddy.Interfaces
{
    public interface INavigation
    {
        void navigateNotificationSettings(); //Android, evtl. iOS
        void navigateNotificationPolicyAccess(); //Android
        void navigateDozeExempt(); //Android
        void navigateHuaweiPowerException(); //Android
        void navigateTelegramChat(int chatID, TelegramPeer.TYPE type);
        void quitApplication();
        bool isTelegramInstalled(); //TODO: Check for third party clients - evtl. advanced options to use alternate client

        void logPermissionSettings();
    }
}
