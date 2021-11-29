using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PagerBuddy.Interfaces {
    public interface INavigation {
        bool isTelegramInstalled();
    }

    public interface IAndroidNavigation : INavigation {
        void navigateNotificationSettings(); //Android, evtl. iOS
        void navigateTelegramChat(int chatID, TelegramPeer.TYPE type);
        void quitApplication();
    }

    public interface IiOSNavigation : INavigation {
    }




}
