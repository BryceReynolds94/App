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

        public enum MENU_PAGE { AboutPage, LogoutUser, NotificationSettings, Share, Login};

        MenuPageViewModel viewModel;
        public MenuPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new MenuPageViewModel();
            viewModel.RequestNavigation += requestNavigation;
        }

        private async void requestNavigation(object sender, MENU_PAGE destination)
        {
            await RootPage.NavigateFromMenu(destination);
        }

    }
}