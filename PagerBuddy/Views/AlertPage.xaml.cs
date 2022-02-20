using PagerBuddy.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using System.IO;

namespace PagerBuddy.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AlertPage : ContentPage
    {    
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly AlertPageViewModel viewModel; 

        private readonly long groupID;
        private readonly TelegramPeer.TYPE peerType;

        private Action stopRingtoneCallback;
        public enum MESSAGING_KEYS { REQUEST_START_PAGE }


        public AlertPage(Alert alert)
        {
            InitializeComponent();

            this.groupID = alert.chatID;
            this.peerType = alert.peerType;
            BindingContext = viewModel = new AlertPageViewModel(alert.title, alert.description, alert.configID, alert.hasPic);
            viewModel.RequestCancel += cancel;
            viewModel.RequestConfirm += confirm;


            startAnimation();
            

            if(Device.RuntimePlatform == Device.Android) {
                IAndroidNotifications notifications = DependencyService.Get<IAndroidNotifications>();
                notifications.closeNotification((int) groupID);
                stopRingtoneCallback = notifications.playChannelRingtone(alert.configID); //Take over sound control
            }

        }

        private async void startAnimation() {
            await MainIcon.ScaleTo(1.3, 1010);
            await MainIcon.ScaleTo(1, 1000);
            startAnimation();
        }

        private void stopRingtone() {
            if(Device.RuntimePlatform == Device.Android && stopRingtoneCallback != null) {
                stopRingtoneCallback.Invoke();
            }
        }

        private void cancel(object sender, EventArgs args)
        {
            stopRingtone();

            if (Device.RuntimePlatform == Device.Android) {
                IAndroidNavigation nav = DependencyService.Get<IAndroidNavigation>();
                nav.quitApplication();
            } else {
                MessagingCenter.Send(this, MESSAGING_KEYS.REQUEST_START_PAGE.ToString());
            }
        }

        private void confirm(object sender, EventArgs args)
        {
            if (Device.RuntimePlatform == Device.Android) {
                IAndroidNavigation nav = DependencyService.Get<IAndroidNavigation>();
                nav.navigateTelegramChat(groupID, peerType);
                nav.quitApplication();
            } else {
                MessagingCenter.Send(this, MESSAGING_KEYS.REQUEST_START_PAGE.ToString());
            }
        }

        protected override void OnDisappearing() {
            base.OnDisappearing();
            cancel(this, null);
        }
    }
}