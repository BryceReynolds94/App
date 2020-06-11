using AlarmManagerT.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace AlarmManagerT.ViewModels
{
    public class ConfigureActionPageViewModel : BaseViewModel
    {
        private AlertConfig alertConfig;
        public Command SaveConfiguration { get; set; }

        public ConfigureActionPageViewModel(AlertConfig alertConfig)
        {
            this.alertConfig = alertConfig;
            Title = "Alert Configuration";
            SaveConfiguration = new Command(() => Next.Invoke(this, null));
        }

        public EventHandler Next;

        public bool SoundActive {
            get => alertConfig.actionSound;
            set => alertConfig.actionSound = value;
        }

        public bool VibrateActive {
            get => alertConfig.actionVibrate;
            set => alertConfig.actionVibrate = value;
        }

        public bool LightActive {
            get => alertConfig.actionLight;
            set => alertConfig.actionLight = value;
        }

    }
}
