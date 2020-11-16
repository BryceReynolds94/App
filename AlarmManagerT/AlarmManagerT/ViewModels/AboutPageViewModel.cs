using AlarmManagerT.Resources;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace AlarmManagerT.ViewModels {
    public class AboutPageViewModel : BaseViewModel {

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public Command DeveloperMode { get; set; }

        private DateTime developerTapStart = DateTime.MinValue;
        private int developerTapCount = 0; 
        public AboutPageViewModel() {

            Title = AppResources.AboutPage_Title;
            DeveloperMode = new Command(() => countDeveloperMode());
        }

        private void countDeveloperMode() {
            if(DateTime.Now.Subtract(developerTapStart).TotalSeconds < 2) {
                developerTapCount++;
            } else {
                developerTapCount = 1;
                developerTapStart = DateTime.Now;
            }

            if(developerTapCount > 4) {
                initiateDeveloperMode();
                developerTapCount = 0;
            }
        }

        private void initiateDeveloperMode() {
            Logger.Info("Developer Mode activated.");
            //TODO: Implement developer mode
        }
    }
}
