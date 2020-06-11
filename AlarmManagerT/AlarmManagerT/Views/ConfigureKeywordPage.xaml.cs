using AlarmManagerT.Models;
using AlarmManagerT.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace AlarmManagerT.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfigureKeywordPage : ContentPage
    {
        private AlertConfig alertConfig;

        private ConfigureKeywordPageViewModel viewModel;

        public ConfigureKeywordPage(AlertConfig alertConfig)
        {
            InitializeComponent();
            this.alertConfig = alertConfig;

            BindingContext = viewModel = new ConfigureKeywordPageViewModel(alertConfig);
            viewModel.Next += keywordConfigureCompleted;

        }

        public async void keywordConfigureCompleted(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ConfigureActionPage(alertConfig));
        }


    }
}