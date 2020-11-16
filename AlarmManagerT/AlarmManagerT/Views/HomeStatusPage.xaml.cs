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

        private CommunicationService client;

        private Collection<AlertConfig> alertList;

        public delegate Task<DateTime> SnoozeTimeHandler(object sender, string title);

        public HomeStatusPage(CommunicationService client)
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
                

            MessagingCenter.Subscribe<ConfigureKeywordPage, AlertConfig>(this, ConfigureKeywordPage.MESSAGING_KEYS.ALERT_CONFIG_SAVED.ToString(), (obj, alertConfig) => alertConfigSaved(alertConfig));
            
            MessagingCenter.Subscribe<AlertStatusViewModel, AlertConfig>(this, AlertStatusViewModel.MESSAGING_KEYS.EDIT_ALERT_CONFIG.ToString(), (obj, alertConfig) => editAlertConfig(alertConfig));
            MessagingCenter.Subscribe<AlertStatusViewModel, AlertConfig>(this, AlertStatusViewModel.MESSAGING_KEYS.DELETE_ALERT_CONFIG.ToString(), (obj, alertConfig) => deleteAlertConfig(alertConfig));
            MessagingCenter.Subscribe<AlertStatusViewModel, Action<DateTime>>(this, AlertStatusViewModel.MESSAGING_KEYS.REQUEST_SNOOZE_TIME.ToString(), (obj, callback) => snoozeTimeRequest(obj, callback));

            MessagingCenter.Subscribe<MainPage>(this, MainPage.MESSAGING_KEYS.LOGOUT_USER.ToString(), (_) => deleteAllAlertConfigs());
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            updateClientErrorStatus(this, null);

            bool allOff = DataService.getConfigValue(DataService.DATA_KEYS.CONFIG_DEACTIVATE_ALL, false);
            bool allSnoozed = DataService.getConfigValue(DataService.DATA_KEYS.CONFIG_SNOOZE_ALL, DateTime.MinValue.Ticks) > DateTime.Now.Ticks;
            viewModel.setWarningState(allOff, allSnoozed);

            if (viewModel.alertList.Count == 0) {
                viewModel.IsBusy = true;
            }
        }

        private async void editAlertConfig(AlertConfig alertConfig)
        {
            await Navigation.PushAsync(new ConfigureGroupPage(client, alertConfig));
        }

        private void deleteAllAlertConfigs() {
            Collection<AlertConfig> configs = getAlertConfigs();
            foreach(AlertConfig config in configs) {
                deleteAlertConfig(config);
            }
        }

        private void deleteAlertConfig(AlertConfig alertConfig)
        {
            DataService.deleteAlertConfig(alertConfig);
            alertList.Remove(alertConfig);
            
            INotifications notifications = DependencyService.Get<INotifications>();
            notifications.removeNotificationChannel(alertConfig);


            viewModel.fillAlertList(alertList);

        }

        private void updateClientErrorStatus(object sender, EventArgs eventArgs)
        {
            CommunicationService.STATUS clientStatus = client.clientStatus;
            viewModel.setErrorState(clientStatus != CommunicationService.STATUS.OFFLINE, clientStatus == CommunicationService.STATUS.AUTHORISED);
        }

        private async void login(object sender, EventArgs eventArgs)
        {
            await Navigation.PushAsync(new LoginPhonePage(client));
        }

        private async void refreshClient(object sender, EventArgs eventArgs)
        {
            await client.reloadConnection();
        }

        private async void addConfig(object sender, EventArgs eventArgs)
        {
            AlertConfig alertConfig = new AlertConfig();
            alertConfig.activeTimeConfig.initDays();
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

        private async void alertConfigSaved(AlertConfig alertConfig)
        {
            INotifications notifications = DependencyService.Get<INotifications>();
            notifications.addNotificationChannel(alertConfig);

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
                await showDNDPermissionPrompt();
                DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DND_PERMISSION, true);
            }
        }

        private async Task showDNDPermissionPrompt()
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
            timeDict.Add(string.Format(AppResources.HomeStatusPage_Snooze_Hours, 3), new TimeSpan(3, 0, 0)); 
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