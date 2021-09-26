﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using PagerBuddy.Models;
using PagerBuddy.Views;
using PagerBuddy.ViewModels;
using PagerBuddy.Services;
using PagerBuddy.Interfaces;
using FFImageLoading.Svg.Forms;
using System.Xml;
using System.Collections.ObjectModel;
using PagerBuddy.Resources;
using Xamarin.Essentials;

using Types = Telega.Rpc.Dto.Types;

namespace PagerBuddy.Views
{

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomeStatusPage : ContentPage
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly HomeStatusPageViewModel viewModel;

        private readonly CommunicationService client;

        private Collection<AlertConfig> alertList;

        public delegate Task<DateTime> SnoozeTimeHandler(object sender, string title);

        public HomeStatusPage(CommunicationService client)
        {
            InitializeComponent();
            this.client = client;
            this.client.StatusChanged += updateClientStatus;

            BindingContext = viewModel = new HomeStatusPageViewModel();
            viewModel.RequestSnoozeTime += getSnoozeTime;
            viewModel.AllDeactivatedStateChanged += saveDeactivatedState;
            viewModel.AllSnoozeStateChanged += saveSnoozeState;
            viewModel.RefreshConfigurationRequest += ;
            viewModel.RequestLogin += login;
            viewModel.RequestRefresh += refreshClient;
            
            MessagingCenter.Subscribe<AlertStatusViewModel, Action<DateTime>>(this, AlertStatusViewModel.MESSAGING_KEYS.REQUEST_SNOOZE_TIME.ToString(), (obj, callback) => snoozeTimeRequest(obj, callback));

            MessagingCenter.Subscribe<MainPage>(this, MainPage.MESSAGING_KEYS.LOGOUT_USER.ToString(), (_) => deleteAllAlertConfigs());

            if (!DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_WELCOME, false)) {
                DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_WELCOME, true);
                _ = showWelcomePrompt();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            bool allOff = DataService.getConfigValue(DataService.DATA_KEYS.CONFIG_DEACTIVATE_ALL, false);
            bool allSnoozed = DataService.getConfigValue(DataService.DATA_KEYS.CONFIG_SNOOZE_ALL, DateTime.MinValue) > DateTime.Now;
            viewModel.setWarningState(allOff, allSnoozed);

            updateClientStatus(this, client.clientStatus);

            alertList = getAlertConfigs();
            viewModel.fillAlertList(alertList);
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
            AlertConfig remove = alertList.First(x => x.id.Equals(alertConfig.id));
            alertList.Remove(remove);
            
            INotifications notifications = DependencyService.Get<INotifications>();
            notifications.removeNotificationChannel(alertConfig);

            viewModel.fillAlertList(alertList);
        }

        private void updateClientStatus(object sender, CommunicationService.STATUS newStatus)
        {
            bool isLoading = newStatus == CommunicationService.STATUS.NEW || newStatus == CommunicationService.STATUS.ONLINE;
            viewModel.setLoadingState(isLoading);

            if (isLoading) { //Suppress warning updates while we are loading
                return;
            }

            bool hasInternet = !(newStatus == CommunicationService.STATUS.OFFLINE);
            bool isAuthorised = newStatus == CommunicationService.STATUS.AUTHORISED;
            viewModel.setErrorState(hasInternet, isAuthorised);
        }

        private async void login(object sender, EventArgs eventArgs)
        {
            await Navigation.PushAsync(new LoginPhonePage(client));
        }

        private async void refreshClient(object sender, EventArgs eventArgs)
        {
            await client.reloadConnection();
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
            DataService.setConfigValue(DataService.DATA_KEYS.CONFIG_SNOOZE_ALL, state);
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

            if (Device.RuntimePlatform == Device.Android && !DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DOZE_EXEMPT, false)) {
                showDozeExemptPrompt();
                DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DOZE_EXEMPT, true);
            }

            if (Device.RuntimePlatform == Device.Android && !DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DND_PERMISSION, false))
            {
                await showDNDPermissionPrompt();
                DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_DND_PERMISSION, true);
            }

            if(Device.RuntimePlatform == Device.Android && DeviceInfo.Manufacturer.Contains("HUAWEI", StringComparison.OrdinalIgnoreCase) && !DataService.getConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_HUAWEI_EXEPTION, false)) {
                await showHuaweiPrompt();
                DataService.setConfigValue(DataService.DATA_KEYS.HAS_PROMPTED_HUAWEI_EXEPTION, true);
            }
        }

        private async Task showDNDPermissionPrompt()
        {
            bool confirmed = await DisplayAlert(AppResources.HomeStatusPage_DNDPermissionPrompt_Title, 
                AppResources.HomeStatusPage_DNDPermissionPrompt_Message, 
                AppResources.HomeStatusPage_DNDPermissionPrompt_Confirm, 
                AppResources.HomeStatusPage_DNDPermissionPrompt_Cancel);

            if (!confirmed) {
                return;
            }

            Interfaces.INavigation navigation = DependencyService.Get<Interfaces.INavigation>();
            navigation.navigateNotificationPolicyAccess();
        }

        private void showDozeExemptPrompt() {
            //https://developer.android.com/training/monitoring-device-state/doze-standby#exemption-cases
            Interfaces.INavigation navigation = DependencyService.Get<Interfaces.INavigation>();
            navigation.navigateDozeExempt();
        }

        private async Task showHuaweiPrompt() {
            bool confirmed = await DisplayAlert(AppResources.HomeStatusPage_HuaweiPrompt_Title,
                AppResources.HomeStatusPage_HuaweiPrompt_Message,
                AppResources.HomeStatusPage_HuaweiPrompt_Confirm,
                AppResources.HomeStatusPage_HuaweiPrompt_Cancel);

            if (!confirmed) {
                return;
            }
            Interfaces.INavigation navigation = DependencyService.Get<Interfaces.INavigation>();
            navigation.navigateHuaweiPowerException();
        }

        private async Task showWelcomePrompt() {
            bool confirmed = await DisplayAlert(AppResources.HomeStatusPage_WelcomePrompt_Title,
                AppResources.HomeStatusPage_WelcomePrompt_Message,
                AppResources.HomeStatusPage_WelcomePrompt_Confirm,
                AppResources.HomeStatusPage_WelcomePrompt_Cancel);

            if (!confirmed) {
                return;
            }
            login(this, null);
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

        private async Task refreshAlertConfigs() {
            Types.Messages.Dialogs rawChatList = await client.getChatList();

            IReadOnlyList<Types.Dialog> dialogList = new List<Types.Dialog>();
            Collection<TelegramPeer> peerCollection = new Collection<TelegramPeer>();

            if (rawChatList == null) {
                Logger.Warn("Retrieving chat list returned null.");
                //TODO: Handle user-facing output in this case
                return;
            } else if (rawChatList.Default != null) {
                Types.Messages.Dialogs.DefaultTag dialogsTag = rawChatList.Default;
                dialogList = dialogsTag.Dialogs;
                peerCollection = TelegramPeer.getPeerCollection(dialogsTag.Chats);
            } else if (rawChatList.Slice != null) {
                Logger.Info("Return type was DialogsSlice. Presumably user has more than 100 active dialogs.");
                Types.Messages.Dialogs.SliceTag dialogsTag = rawChatList.Slice;
                dialogList = dialogsTag.Dialogs;
                peerCollection = TelegramPeer.getPeerCollection(dialogsTag.Chats);
            } else if (rawChatList.NotModified != null) {
                Logger.Warn("Return type was DialogsNotModified. This case is not implemented and will be treated as empty chat list.");
            }

            if (dialogList.Count < 1) {
                Logger.Warn("Chat list was empty.");
                //TODO: Handle user-facing output in this case
                return;
            }

            Collection<AlertConfig> configList = new Collection<AlertConfig>();

            foreach (Types.Dialog dialog in dialogList) {
                if (dialog.Default == null) {
                    Logger.Info("Dialog list contained empty member.");
                    continue;
                }

                Types.Peer peer = dialog.Default.Peer;

                int id;
                if (peer.Channel != null) {
                    id = peer.Channel.ChannelId;
                } else if (peer.Chat != null) {
                    id = peer.Chat.ChatId;
                } else if (peer.User != null) {
                    id = peer.User.UserId;
                } else {
                    Logger.Warn("Peer type was not found and will be ignored.");
                    continue;
                }

                TelegramPeer detailPeer = peerCollection.FirstOrDefault((TelegramPeer x) => x.id == id);
                if (detailPeer == null || detailPeer.id != id) {
                    Logger.Info("Details for a peer could not be found in peer collection and will be ignored.");
                    continue;
                }

                if (detailPeer.photoLocation != null) {
                    _ = detailPeer.loadImage(client); //Do not wait for image loading to avoid blocking
                }

                AlertConfig alertConfig = AlertConfig.findExistingConfig(detailPeer.id);
                if (alertConfig == null) {
                    alertConfig = new AlertConfig(detailPeer);
                }
                configList.Add(alertConfig);
            }

            viewModel.fillAlertList(configList);
        }

        private async void snoozeTimeRequest(object sender, Action<DateTime> callback)
        {
            DateTime result = await getSnoozeTime(sender, AppResources.HomeStatusPage_IndividualSnooze_Prompt);
            callback(result);
        }

        private async Task<DateTime> getSnoozeTime(object sender, string title)
        {
            string cancelText = AppResources.HomeStatusPage_Snooze_Cancel;
            Dictionary<string, TimeSpan> timeDict = new Dictionary<string, TimeSpan> {
                { string.Format(AppResources.HomeStatusPage_Snooze_Hours, 3), new TimeSpan(3, 0, 0) },
                { string.Format(AppResources.HomeStatusPage_Snooze_Hours, 8), new TimeSpan(8, 0, 0) },
                { string.Format(AppResources.HomeStatusPage_Snooze_Hours, 12), new TimeSpan(12, 0, 0) },
                { AppResources.HomeStatusPage_Snooze_Day, new TimeSpan(1, 0, 0, 0) },
                { string.Format(AppResources.HomeStatusPage_Snooze_Days, 2), new TimeSpan(2, 0, 0, 0) },
                { string.Format(AppResources.HomeStatusPage_Snooze_Days, 3), new TimeSpan(3, 0, 0, 0) },
                { AppResources.HomeStatusPage_Snooze_Week, new TimeSpan(7, 0, 0, 0) },
                { string.Format(AppResources.HomeStatusPage_Snooze_Weeks, 2), new TimeSpan(14, 0, 0, 0) },
                { AppResources.HomeStatusPage_Snooze_Month, new TimeSpan(30, 0, 0, 0) }
            };

            string result = await DisplayActionSheet(title, cancelText, null, timeDict.Keys.ToArray());

            if (!timeDict.TryGetValue(result, out TimeSpan selection))
            {
                return DateTime.MinValue;
            }

            return DateTime.Now.Add(selection);
        }



    }
}