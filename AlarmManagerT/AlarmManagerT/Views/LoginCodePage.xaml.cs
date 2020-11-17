using AlarmManagerT.Services;
using AlarmManagerT.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static AlarmManagerT.Services.ClientExceptions;

namespace AlarmManagerT.Views
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
                Navigation.InsertPageBefore(new LoginPasswordPage(client), this);
                await Navigation.PopAsync();
                return;

            }else if(result != TStatus.OK){
                viewModel.updateErrorState(result);
                return;
            } 
            await Navigation.PopToRootAsync();
        }
    }
}