using PagerBuddy.Services;
using PagerBuddy.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static PagerBuddy.Services.ClientExceptions;

namespace PagerBuddy.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPhonePage : ContentPage
    {
        CommunicationService client;
        LoginPhonePageViewModel viewModel;

        public LoginPhonePage(CommunicationService client)
        {
            InitializeComponent();
            this.client = client;

            BindingContext = viewModel = new LoginPhonePageViewModel();
            viewModel.RequestClientLogin += performLogin;
        }

        private async void performLogin(object sender, string phoneNumber)
        {
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