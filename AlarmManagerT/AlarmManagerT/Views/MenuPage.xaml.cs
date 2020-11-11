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

        }

        private async void requestNavigation(object sender, MENU_PAGE destination)
        {
            await RootPage.NavigateFromMenu(destination);
        }

        private void requestNotificationSettings(object sender, EventArgs _)
        {
            Models.INavigation navigationInterface = DependencyService.Get<Models.INavigation>();
            navigationInterface.navigateNotificationPolicyAccess();
        }
    }
}