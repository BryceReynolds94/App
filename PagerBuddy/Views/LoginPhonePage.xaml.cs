using PagerBuddy.Services;
using PagerBuddy.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static PagerBuddy.Services.ClientExceptions;

namespace PagerBuddy.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPhonePage : ContentPage
    {
        private readonly CommunicationService client;
        private readonly LoginPhonePageViewModel viewModel;

        public LoginPhonePage(CommunicationService client)
        {
            InitializeComponent();
            this.client = client;

            BindingContext = viewModel = new LoginPhonePageViewModel();
            viewModel.RequestClientLogin += performLogin;
            viewModel.RequestTelegramLink += installTelegram;
        }

        private async void installTelegram(object sender, EventArgs args) {
            string url = "";
            if (Device.RuntimePlatform == Device.Android) {
                url = "https://play.google.com/store/apps/details?id=org.telegram.messenger";
            } else if(Device.RuntimePlatform == Device.iOS) {
                url = "https://apps.apple.com/us/app/telegram-messenger/id686449807";
            }
            await Launcher.OpenAsync(url);
        }

        private async void performLogin(object sender, string phoneNumber)
        {
            if(phoneNumber == "+12099999999") {
                DataService.setConfigValue(DataService.DATA_KEYS.MOCK_ACCOUNT, true);
                await Navigation.PushAsync(new LoginCodePage(client));
                return;
            }

            TStatus result = await client.requestCode(phoneNumber);
            viewModel.changeErrorStatus(result);
            viewModel.setWaitStatus(false);
            if (result != TStatus.OK)
            {
                return;
            }
            await Navigation.PushAsync(new LoginCodePage(client));
        }


    }
}