using AlarmManagerT.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static AlarmManagerT.Services.ClientExceptions;

namespace AlarmManagerT.ViewModels {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPasswordPage : ContentPage {

        private LoginPasswordPageViewModel viewModel;
        private CommunicationService client;
        public LoginPasswordPage(CommunicationService client) {
            InitializeComponent();

            this.client = client; 

            BindingContext = viewModel = new LoginPasswordPageViewModel();
            viewModel.RequestAuthenticate += performAuthentication;
        }

        private async void performAuthentication(object sender, string password) {

            TStatus result = await client.loginWithPassword(password);
            if(result != TStatus.OK) {
                viewModel.updateErrorState(result);
            } else {
                await Navigation.PopToRootAsync();
            }
        }

    }
}