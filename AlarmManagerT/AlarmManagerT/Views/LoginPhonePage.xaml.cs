using AlarmManagerT.Services;
using AlarmManagerT.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static AlarmManagerT.Services.ClientExceptions;

namespace AlarmManagerT.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPhonePage : ContentPage
    {
        //TODO: Implement next on keyboard confirm button

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
            //TODO: Check valid phoneNumber
            TStatus result = await client.requestCode(phoneNumber);
            viewModel.changeErrorStatus(result);
            if (result != TStatus.OK)
            {
                return;
            }
            await Navigation.PushAsync(new LoginCodePage(client));
        }


    }
}