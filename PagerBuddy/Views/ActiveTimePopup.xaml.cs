using PagerBuddy.Services;
using PagerBuddy.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PagerBuddy.Views {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ActiveTimePopup : ContentPage {

        private readonly ActiveTimePopupViewModel viewModel;

        public ActiveTimePopup() {
            InitializeComponent();

            TimeSpan fromTime = TimeSpan.FromTicks(DataService.getConfigValue(DataService.DATA_KEYS.ACTIVE_TIME_FROM, TimeSpan.Zero.Ticks));
            TimeSpan toTime = TimeSpan.FromTicks(DataService.getConfigValue(DataService.DATA_KEYS.ACTIVE_TIME_TO, TimeSpan.FromMinutes(1439).Ticks));
            bool invertTime = DataService.getConfigValue(DataService.DATA_KEYS.ACTIVE_TIME_INVERT, false);
            Collection<DayOfWeek> activeDays = DataService.activeDays;


            BindingContext = viewModel = new ActiveTimePopupViewModel(activeDays, fromTime, toTime, invertTime);
            viewModel.RequestCancel += cancel;
            viewModel.ActiveTimeResult += activeTimeResult;
        }

        private void cancel(object sender, EventArgs args) {
            Navigation.PopModalAsync(false);
        }

        private void activeTimeResult(Collection<DayOfWeek> dayList, TimeSpan fromTime, TimeSpan toTime, bool invertTime) {
            DataService.setConfigValue(DataService.DATA_KEYS.ACTIVE_TIME_FROM, fromTime.Ticks);
            DataService.setConfigValue(DataService.DATA_KEYS.ACTIVE_TIME_TO, toTime.Ticks);
            DataService.setConfigValue(DataService.DATA_KEYS.ACTIVE_TIME_INVERT, invertTime);
            DataService.activeDays = dayList;

            cancel(this, null);
        }
    }
}