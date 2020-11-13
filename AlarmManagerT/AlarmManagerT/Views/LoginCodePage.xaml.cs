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
        //TODO: Implement next on keyboard confirm button. Use complted event: (https://stackoverflow.com/questions/46292614/xamarin-forms-how-to-detect-enter-press-in-entry-inside-a-view-model)

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
            //TODO: Check valid code before commit
            TStatus result = await client.confirmCode(code);
            if(result != TStatus.OK)
            {
                viewModel.updateErrorState(result);
                return;
            } 
            await Navigation.PopToRootAsync();
        }
    }
}