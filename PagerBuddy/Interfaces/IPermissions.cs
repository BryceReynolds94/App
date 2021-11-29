using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace PagerBuddy.Interfaces
{
    public interface IPermissions {
        void logPermissionSettings();
    }

    public interface IiOSPermissions : IPermissions {
        Task<bool> requestNotificationPermission();
    }

    public interface IAndroidPermissions : IPermissions {
        void permissionNotificationPolicyAccess(); //Android
        void permissionDozeExempt(); //Android
        void permissionHuaweiPowerException(); //Android
    }


}
