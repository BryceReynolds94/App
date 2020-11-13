using AlarmManagerT.Models;
using AlarmManagerT.Services;
using AlarmManagerT.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace AlarmManagerT.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MenuPage : ContentPage
    {
        MainPage RootPage { get => Application.Current.MainPage as MainPage; }

        public enum MENU_PAGE { AccountPage, AboutPage};

        MenuPageViewModel viewModel;
        public MenuPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new MenuPageViewModel();
            viewModel.RequestNavigation += requestNavigation;
            viewModel.RequestNotificationSettings += requestNotificationSettings;
            viewModel.RequestAbout += requestAbout;
            viewModel.RequestShare += requestShare;
            viewModel.RequestLogout += requestLogout;

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

        private void requestAbout(object sender, EventArgs _)
        {
            //TODO: Implement About Page
        }

        private void requestLogout(object sender, EventArgs _) {
            //TODO: Implement Logout
        }

        private void requestShare(object sender, EventArgs _)
        {
            Interfaces.INavigation navigationInterface = DependencyService.Get<Interfaces.INavigation>();
            navigationInterface.navigateShare("SAMPLE MESSAGE"); //TODO: Write Message to share
        }
    }
}