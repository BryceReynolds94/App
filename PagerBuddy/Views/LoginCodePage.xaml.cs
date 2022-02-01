using PagerBuddy.Models;
using PagerBuddy.Services;
using PagerBuddy.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static PagerBuddy.Services.ClientExceptions;

namespace PagerBuddy.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginCodePage : ContentPage
    {
        private readonly CommunicationService client;
        private readonly LoginCodePageViewModel viewModel;

        private bool isMock;
        public LoginCodePage(CommunicationService client)
        {
            InitializeComponent();
            this.client = client;

            this.isMock = DataService.getConfigValue(DataService.DATA_KEYS.MOCK_ACCOUNT, false);

            BindingContext = viewModel = new LoginCodePageViewModel();
            viewModel.RequestAuthenticate += performAuthentication;
        }

        private async void performAuthentication(object sender, string code)
        {
            if (isMock) {
                client.mockLogin();
                await Navigation.PopToRootAsync();
                return;
            }

            TStatus result = await client.confirmCode(code);
            if(result == TStatus.PASSWORD_REQUIRED) {
                viewModel.setWaitStatus(false);
                Navigation.InsertPageBefore(new LoginPasswordPage(client), this);
                await Navigation.PopAsync();
                return;

            }else if(result != TStatus.OK){
                viewModel.updateErrorState(result);
                viewModel.setWaitStatus(false);
                return;
            } 
            await Navigation.PopToRootAsync();
        }
    }
}