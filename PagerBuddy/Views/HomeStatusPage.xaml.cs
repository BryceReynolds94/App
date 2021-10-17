using System;
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

        //TODO Later: Implement alert content filtering (get from Server?)

        public HomeStatusPage(CommunicationService client)
        {
            InitializeComponent();
            this.client = client;
            this.client.StatusChanged += updateClientStatus;

            BindingContext = viewModel = new HomeStatusPageViewModel();
            viewModel.RequestSnoozeTime += getSnoozeTime;
            viewModel.AllDeactivatedStateChanged += saveDeactivatedState;
            viewModel.AllSnoozeStateChanged += saveSnoozeState;
            viewModel.RefreshConfigurationRequest += refreshAlertConfigs;
            viewModel.RequestLogin += login;
            viewModel.RequestTimeConfig += timeConfig;

            MessagingCenter.Subscribe<AlertStatusViewModel>(this, AlertStatusViewModel.MESSAGING_KEYS.ALERT_CONFIG_CHANGED.ToString(), async (_) => await alertConfigToggled());

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
            viewModel.setDeactivateState(allOff, true);
            _ = viewModel.setSnoozeState(allSnoozed, true);

            updateClientStatus(this, client.clientStatus);

            viewModel.fillAlertList(getAlertConfigs()); //First fill view with current state to avoid long wait

            if (client.clientStatus == CommunicationService.STATUS.AUTHORISED) { //Do not bother if we are not connected yet
                refreshAlertConfigs(this, null); //Check for config updates
            }
        }

        private async Task alertConfigToggled() {
            if(client.clientStatus != CommunicationService.STATUS.AUTHORISED && client.clientStatus > CommunicationService.STATUS.ONLINE) {
                //User is not logged in - do nothing.
                return;
            }else if(client.clientStatus < CommunicationService.STATUS.WAIT_PHONE) {
                Logger.Info("The client is not connected. Will retry sending updates to server at a later time...");
                //Client is not connected (yet) - retry later
                //TODO: Implement retry with some back-off system. This must also work if killed.
                return;
            }

            Logger.Info("An alert config was changed by the user. Informing pagerbuddy server.");
            Collection<AlertConfig> configList = getAlertConfigs();
            bool result = await client.sendServerRequest(configList);
        }

        private void updateClientStatus(object sender, CommunicationService.STATUS newStatus)
        {
            bool isLoading = newStatus == CommunicationService.STATUS.NEW || newStatus == CommunicationService.STATUS.ONLINE;
            if (isLoading) { //Suppress warning updates while we are loading
                return;
            }

            bool hasInternet = !(newStatus == CommunicationService.STATUS.OFFLINE);
            bool isAuthorised = newStatus == CommunicationService.STATUS.AUTHORISED;
            viewModel.setErrorState(hasInternet, isAuthorised);

            if(newStatus == CommunicationService.STATUS.AUTHORISED) {
                refreshAlertConfigs(this, null); //Check for config updates as soon as we go online
            }
        }

        private async void timeConfig(object sender, EventArgs eventArgs) {
            await Navigation.PushModalAsync(new ActiveTimePopup(), false);
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
        }

        private void saveSnoozeState(object sender, DateTime state)
        {
            DataService.setConfigValue(DataService.DATA_KEYS.CONFIG_SNOOZE_ALL, state);
        }

        private async Task alertConfigsChanged(Collection<AlertConfig> configList){
            INotifications notifications = DependencyService.Get<INotifications>();
            notifications.UpdateNotificationChannels(configList);

            bool result = await client.sendServerRequest(configList); //TODO: Implement retry

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
                AlertConfig config = DataService.getAlertConfig(id, null);
                if(config != null) {
                    configList.Add(config);
                }
            }
            return configList;
        }

        private bool hasListChanged(Collection<AlertConfig> newList) {
            Collection<string> oldList = DataService.getConfigList();
            bool simpleSame = newList.All((config) => oldList.Contains(config.id)) && newList.Count == oldList.Count;
            return !simpleSame;
        }

        private async void refreshAlertConfigs(object sender, EventArgs args) {

            Types.Messages.Chats rawChatList = await client.getChatList();

            IReadOnlyList<Types.Chat> chatList = new List<Types.Chat>();
            if (rawChatList == null) {
                Logger.Warn("Retrieving chat list returned null.");
                return;
            } else if (rawChatList.Default != null) {
                Types.Messages.Chats.DefaultTag chatsTag = rawChatList.Default;
                chatList = chatsTag.Chats;
            } else if (rawChatList.Slice != null) {
                Logger.Info("Return type was ChatsSlice. Presumably user has more than 100 active chats.");
                Types.Messages.Chats.SliceTag chatsTag = rawChatList.Slice;
                chatList = chatsTag.Chats;
            }

            Collection<TelegramPeer> peerCollection = TelegramPeer.getPeerCollection(chatList);


            if (chatList.Count < 1) {
                Logger.Info("Chat list was empty.");
            }

            Collection<AlertConfig> configList = new Collection<AlertConfig>();

            foreach(TelegramPeer peer in peerCollection) {
                if (peer.photoLocation != null) {
                    await peer.loadImage(client);
                }

                AlertConfig alertConfig = AlertConfig.findExistingConfig(peer.id);
                if(alertConfig != null) {
                    alertConfig.triggerGroup = peer;
                } else {
                    alertConfig = new AlertConfig(peer);
                }
                alertConfig.saveChanges(true);
                configList.Add(alertConfig);
            }

            viewModel.fillAlertList(configList);

            if (hasListChanged(configList)) { //If the alert list has changed, subscribe to PagerBuddy-Server with new list
                await alertConfigsChanged(configList);
            }

        }

        private async Task<DateTime> getSnoozeTime(object sender, EventArgs args)
        {
            //TODO Later: Possibly replace this with a date & time picker
            string title = AppResources.HomeStatusPage_Snooze_Prompt;
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