using AlarmManagerT.Models;
using AlarmManagerT.Resources;
using AlarmManagerT.Services;
using AlarmManagerT.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using TeleSharp.TL;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace AlarmManagerT.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MenuPage : ContentPage
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        MainPage RootPage { get => Application.Current.MainPage as MainPage; }

        public enum MENU_PAGE { AboutPage};

        MenuPageViewModel viewModel;
        public MenuPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new MenuPageViewModel();
            viewModel.RequestNavigation += requestNavigation;
            viewModel.RequestNotificationSettings += requestNotificationSettings;
            viewModel.RequestShare += requestShare;
            viewModel.RequestLogout += requestLogout;

            //updateUser(client); //TODO: Solve refresh somewehere else - on app load?
        }

        private async void requestNavigation(object sender, MENU_PAGE destination)
        {
            await RootPage.NavigateFromMenu(destination);
        }

        private void requestNotificationSettings(object sender, EventArgs _)
        {
            Interfaces.INavigation navigationInterface = DependencyService.Get<Interfaces.INavigation>();
            navigationInterface.navigateNotificationSettings();
        }

        private async void requestLogout(object sender, EventArgs _) {
            bool result = await DisplayAlert(AppResources.MenuPage_LogoutPrompt_Title, AppResources.MenuPage_LogoutPrompt_Text, AppResources.MenuPage_LogoutPrompt_Confirm, AppResources.MenuPage_LogoutPrompt_Cancel);

            if (result) {
                Logger.Info("User requested logout.");
                //TODO: Implement Logout
            }
        }

        private void requestShare(object sender, EventArgs _)
        {
            Interfaces.INavigation navigationInterface = DependencyService.Get<Interfaces.INavigation>();
            navigationInterface.navigateShare("SAMPLE MESSAGE"); //TODO: Write Message to share
        }

        private async void updateUser(CommunicationService client) {
            TLUser user = await client.getUser();
            client.saveUserData(user);
        }

    }
}