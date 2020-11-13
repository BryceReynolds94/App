using AlarmManagerT.Interfaces;
using AlarmManagerT.Models;
using AlarmManagerT.Services;
using AlarmManagerT.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

//TODO: RadioButtons are currently broken, solution expected in XF 5.0 very soon (alt. revert to 4.7)
//TODO: Update to XF 5.0

namespace AlarmManagerT.Views
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
            if (alertConfig.triggerGroup.hasImage)
            {
                DataService.saveProfilePic(alertConfig.id, alertConfig.triggerGroup.image);
            }
            alertConfig.triggerGroup.image = null; //clear because the image cannot be serialised on save
            alertConfig.saveChanges();

            MessagingCenter.Send(this, MESSAGING_KEYS.ALERT_CONFIG_SAVED.ToString(), alertConfig);

            await Navigation.PopToRootAsync();
        }


    }
}