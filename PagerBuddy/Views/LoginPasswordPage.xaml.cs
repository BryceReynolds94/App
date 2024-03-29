﻿using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static PagerBuddy.Services.ClientExceptions;

namespace PagerBuddy.ViewModels {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPasswordPage : ContentPage {

        private readonly LoginPasswordPageViewModel viewModel;
        private readonly CommunicationService client;
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
                viewModel.setWaitStatus(false);
            } else {
                await Navigation.PopToRootAsync();
            }
        }

    }
}