﻿using PagerBuddy.Models;
using PagerBuddy.Resources;
using PagerBuddy.Services;
using PagerBuddy.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PagerBuddy.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MenuPage : ContentPage
    {
        MainPage RootPage { get => Application.Current.MainPage as MainPage; }

        public enum MENU_PAGE { AboutPage, LogoutUser, NotificationSettings, Share, Login, Website};

        private readonly MenuPageViewModel viewModel;
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