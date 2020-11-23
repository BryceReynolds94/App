using PagerBuddy.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using PagerBuddy.Interfaces;
using INavigation = PagerBuddy.Interfaces.INavigation;
using PagerBuddy.Models;
using System.IO;

namespace PagerBuddy.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AlertPage : ContentPage
    {      

        AlertPageViewModel viewModel;

        private int groupID;

        public AlertPage(Alert alert)
        {
            InitializeComponent();

            this.groupID = alert.chatID;
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
            INavigation nav = DependencyService.Get<INavigation>();
            nav.quitApplication(); 
        }

        private void confirm(object sender, EventArgs args)
        {
            INavigation nav = DependencyService.Get<INavigation>();
            nav.navigateTelegramChat(groupID);
        }
    }
}