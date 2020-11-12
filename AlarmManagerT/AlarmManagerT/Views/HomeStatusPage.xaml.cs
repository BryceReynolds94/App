using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using AlarmManagerT.Models;
using AlarmManagerT.Views;
using AlarmManagerT.ViewModels;
using AlarmManagerT.Services;
using AlarmManagerT.Interfaces;
using FFImageLoading.Svg.Forms;
using System.Xml;
using System.Collections.ObjectModel;
using Plugin.FirebasePushNotification;
using AlarmManagerT.Resources;

namespace AlarmManagerT.Views
{ 

    [DesignTimeVisible(false)]
    public partial class HomeStatusPage : ContentPage
    {
        HomeStatusPageViewModel viewModel;

        private MyClient client;

        private Collection<AlertConfig> alertList;

        public delegate Task<DateTime> SnoozeTimeHandler(object sender, string title);

        public HomeStatusPage(MyClient client)
        {
            InitializeComponent();
            this.client = client;
            this.client.StatusChanged += updateClientErrorStatus;


            alertList = getAlertConfigs();
            BindingContext = viewModel = new HomeStatusPageViewModel(alertList);
            viewModel.RequestSnoozeTime += getSnoozeTime;
            viewModel.AllDeactivatedStateChanged += saveDeactivatedState;
            viewModel.AllSnoozeStateChanged += saveSnoozeState;
            viewModel.AddConfigurationRequest += addConfig;
            viewModel.RequestLogin += login;
            viewModel.RequestRefresh += refreshClient;
                

            MessagingCenter.Subscribe<ConfigureKeywordPage, AlertConfig>(this, "AlertConfigSaved", (obj, alertConfig) => alertConfigSaved(alertConfig));
            
            MessagingCenter.Subscribe<AlertStatusViewModel, AlertConfig>(this, "EditAlertConfig", (obj, alertConfig) => editAlertConfig(alertConfig));
            MessagingCenter.Subscribe<AlertStatusViewModel, AlertConfig>(this, "DeleteAlertConfig", (obj, alertConfig) => deleteAlertConfig(alertConfig));
            MessagingCenter.Subscribe<AlertStatusViewModel, Action<DateTime>>(this, "RequestSnoozeTime", (obj, callback) => snoozeTimeRequest(obj, callback));
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            updateClientErrorStatus(this, null);

            bool allOff = DataService.getConfigValue(DataService.DATA_KEYS.CONFIG_DEACTIVATE_ALL, false);
            bool allSnoozed = DataService.getConfigValue(DataService.DATA_KEYS.CONFIG_SNOOZE_ALL, DateTime.MinValue.Ticks) > DateTime.Now.Ticks;
            viewModel.setWarningState(allOff, allSnoozed);

            if (viewModel.alertList.Count == 0)
                viewModel.IsBusy = true;
        }

        private async void editAlertConfig(AlertConfig alertConfig)
        {
            await Navigation.PushAsync(new ConfigureGroupPage(client, alertConfig));
        }

        private void deleteAlertConfig(AlertConfig alertConfig)
        {
            DataService.deleteAlertConfig(alertConfig);
            alertList.Remove(alertConfig);
            viewModel.fillAlertList(alertList);

        }

        private void updateClientErrorStatus(object sender, EventArgs eventArgs)
        {
            MyClient.STATUS clientStatus = client.getClientStatus();
            viewModel.setErrorState(clientStatus != MyClient.STATUS.OFFLINE, clientStatus == MyClient.STATUS.AUTHORISED);
        }

        private async void login(object sender, EventArgs eventArgs)
        {
            await Navigation.PushAsync(new LoginPhonePage(client));
        }

        private async void refreshClient(object sender, EventArgs eventArgs)
        {
            //Retry connection
            await client.connectClient();
        }

        private async void addConfig(object sender, EventArgs eventArgs)
        {
            AlertConfig alertConfig = new AlertConfig();
            await Navigation.PushAsync(new ConfigureGroupPage(client, alertConfig));
        }

        private void saveDeactivatedState(object sender, bool state)
        {
            DataService.setConfigValue(DataService.DATA_KEYS.CONFIG_DEACTIVATE_ALL, state);
            if (state)
            {
                saveSnoozeState(this, DateTime.MinValue); //clear snooze when all is deactivated
            }
        }

        private void saveSnoozeState(object sender, DateTime state)
        {
            DataService.setConfigValue(DataService.DATA_KEYS.CONFIG_SNOOZE_ALL, state.Ticks);
        }

        private void alertConfigSaved(AlertConfig alertConfig)
        {
            if (!alertList.Contains(alertConfig))
            {
                alertList.Add(alertConfig);
            }
            else
            {
                alertList = getAlertConfigs();
            }
            viewModel.fillAlertList(alertList);

            if(Device.RuntimePlatform == Device.Android && !DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DND_PERMISSION, false))
            {
                showDNDPermissionPrompt();
                DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DND_PERMISSION, true);
            }
        }

        private async void showDNDPermissionPrompt()
        {
            await DisplayAlert(AppResources.HomeStatusPage_DNDPermissionPrompt_Title, AppResources.HomeStatusPage_DNDPermissionPrompt_Message, AppResources.HomeStatusPage_DND_PermissionPrompt_Confirm);

            Interfaces.INavigation navigation = DependencyService.Get<Interfaces.INavigation>();
            navigation.navigateNotificationPolicyAccess();
        }

        private Collection<AlertConfig> getAlertConfigs()
        {
            Collection<AlertConfig> configList = new Collection<AlertConfig>();
            Collection<string> configIDs = DataService.getConfigList();
            foreach(string id in configIDs)
            {
                configList.Add(DataService.getAlertConfig(id));
            }
            return configList;
        }

        private async void snoozeTimeRequest(object sender, Action<DateTime> callback)
        {
            DateTime result = await getSnoozeTime(sender, AppResources.HomeStatusPage_IndividualSnooze_Prompt);
            callback(result);
        }

        private async Task<DateTime> getSnoozeTime(object sender, string title)
        {
            string cancelText = AppResources.HomeStatusPage_Snooze_Cancel;
            Dictionary<string, TimeSpan> timeDict = new Dictionary<string, TimeSpan>();
            timeDict.Add(string.Format(AppResources.HomeStatusPage_Snooze_Hours, 3), new TimeSpan(3, 0, 0)); //TODO: Check format strings work
            timeDict.Add(string.Format(AppResources.HomeStatusPage_Snooze_Hours, 12), new TimeSpan(12, 0, 0));
            timeDict.Add(AppResources.HomeStatusPage_Snooze_Day, new TimeSpan(1, 0, 0, 0));
            timeDict.Add(string.Format(AppResources.HomeStatusPage_Snooze_Days, 2), new TimeSpan(2, 0, 0, 0));
            timeDict.Add(string.Format(AppResources.HomeStatusPage_Snooze_Days, 3), new TimeSpan(3, 0, 0, 0));
            timeDict.Add(AppResources.HomeStatusPage_Snooze_Week, new TimeSpan(7, 0, 0, 0));
            timeDict.Add(string.Format(AppResources.HomeStatusPage_Snooze_Weeks, 2), new TimeSpan(14, 0, 0, 0));
            timeDict.Add(AppResources.HomeStatusPage_Snooze_Month, new TimeSpan(30, 0, 0, 0));

            string result = await DisplayActionSheet(title, cancelText, null, timeDict.Keys.ToArray());

            TimeSpan selection;
            if (!timeDict.TryGetValue(result, out selection))
            {
                return DateTime.MinValue;
            }

            return DateTime.Now.Add(selection);
        }



    }
}