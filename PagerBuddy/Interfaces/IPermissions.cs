using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PagerBuddy.Interfaces
{
    public interface IPermissions {
        void logPermissionSettings();

        Task checkAlertPermissions(Page currentView, bool forceReprompt = false);
    }

    public interface IiOSPermissions : IPermissions {
        Task<bool> requestNotificationPermission();
    }

    public interface IAndroidPermissions : IPermissions {
    }


}
