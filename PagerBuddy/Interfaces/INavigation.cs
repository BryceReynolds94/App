using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PagerBuddy.Interfaces {
    public interface INavigation {
        bool isTelegramInstalled();

        void navigateNotificationSettings();
    }

    public interface IAndroidNavigation : INavigation {
        void navigateTelegramChat(long chatID, TelegramPeer.TYPE type);
        void quitApplication();
    }

    public interface IiOSNavigation : INavigation {
    }




}
