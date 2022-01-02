using PagerBuddy.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using PagerBuddy.Interfaces;
using IAndroidNavigation = PagerBuddy.Interfaces.IAndroidNavigation;
using PagerBuddy.Models;
using System.IO;

namespace PagerBuddy.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AlertPage : ContentPage
    {     
        private readonly AlertPageViewModel viewModel; 

        private readonly int groupID;
        private readonly TelegramPeer.TYPE peerType;

        public AlertPage(Alert alert)
        {
            InitializeComponent();

            this.groupID = alert.chatID;
            this.peerType = alert.peerType;
            BindingContext = viewModel = new AlertPageViewModel(alert.title, alert.text, alert.configID, alert.hasPic);
            viewModel.RequestCancel += cancel;
            viewModel.RequestConfirm += confirm;

            startAnimation();
        }

        private async void startAnimation() {
            await MainIcon.ScaleTo(1.3, 1010);
            await MainIcon.ScaleTo(1, 1000);
            startAnimation();
        }

        private void cancel(object sender, EventArgs args)
        {
            IAndroidNotification notifications = DependencyService.Get<IAndroidNotification>();
            notifications.closeNotification(groupID);

            IAndroidNavigation nav = DependencyService.Get<IAndroidNavigation>();
            nav.quitApplication(); 
        }

        private void confirm(object sender, EventArgs args)
        {
            IAndroidNotification notifications = DependencyService.Get<IAndroidNotification>();
            notifications.closeNotification(groupID);

            IAndroidNavigation nav = DependencyService.Get<IAndroidNavigation>();
            nav.navigateTelegramChat(groupID, peerType);
            nav.quitApplication();
        }

        protected override void OnDisappearing() {
            base.OnDisappearing();
            cancel(this, null);
        }
    }
}