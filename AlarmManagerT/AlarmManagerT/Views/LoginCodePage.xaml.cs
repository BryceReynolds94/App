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
        private CommunicationService client;

        private LoginCodePageViewModel viewModel;
        public LoginCodePage(CommunicationService client)
        {
            InitializeComponent();
            this.client = client;

            BindingContext = viewModel = new LoginCodePageViewModel();
            viewModel.RequestAuthenticate += performAuthentication;
        }

        private async void performAuthentication(object sender, string code)
        {
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