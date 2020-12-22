using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using PagerBuddy.Services;
using PagerBuddy.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;


namespace PagerBuddy.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfigureKeywordPage : ContentPage
    {
        private AlertConfig alertConfig;

        private ConfigureKeywordPageViewModel viewModel;

        public enum MESSAGING_KEYS { ALERT_CONFIG_SAVED};

        public ConfigureKeywordPage(AlertConfig alertConfig)
        {
            InitializeComponent();
            this.alertConfig = alertConfig;

            BindingContext = viewModel = new ConfigureKeywordPageViewModel(alertConfig);
            viewModel.Next += keywordConfigureCompleted;

        }

        public async void keywordConfigureCompleted(object sender, EventArgs e)
        {
            alertConfig.saveChanges(true);
            MessagingCenter.Send(this, MESSAGING_KEYS.ALERT_CONFIG_SAVED.ToString(), alertConfig);
            await Navigation.PopToRootAsync();
        }


    }
}