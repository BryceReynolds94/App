using PagerBuddy.Models;
using PagerBuddy.Services;
using PagerBuddy.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleSharp.TL;
using TeleSharp.TL.Upload;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PagerBuddy.Views {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfigureGroupPage : ContentPage {

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private ConfigureGroupPageViewModel viewModel;

        private AlertConfig alertConfig;

        private CommunicationService client;

        private bool reloadListOnAppearing = false;

        public ConfigureGroupPage(CommunicationService client, AlertConfig alertConfig) {
            InitializeComponent();

            this.alertConfig = alertConfig;
            this.client = client;

            BindingContext = viewModel = new ConfigureGroupPageViewModel();
            viewModel.GroupSelectionMade += groupSelected;
            viewModel.ReloadGroupList += refreshGroupList;
        }

        private async void groupSelected(object sender, Group selectedGroup) {
            alertConfig.triggerGroup = selectedGroup;
            await Navigation.PushAsync(new ConfigureKeywordPage(alertConfig));
        }

        private async void refreshGroupList(object sender, Action completedCallback) {
            viewModel.clearGroupList();

            TLVector<TLAbsChat> chatList = await client.getChatList();

            if(chatList.Count < 1) {
                viewModel.IsBusy = false;
                Logger.Warn("Retrieving chat list returned no result.");

                //check is client still available
                if(client.clientStatus != CommunicationService.STATUS.AUTHORISED) {
                    Logger.Info("Client is not authorised. Returning to HomePage. Client status: " + client.clientStatus.ToString());
                    await Navigation.PopAsync();
                } else {
                    viewModel.AreChatsEmpty = true;
                }
                completedCallback();
                return;
            }
            viewModel.AreChatsEmpty = false;

            foreach (TLChat chat in chatList) {
                int id = chat.Id;
                string name = chat.Title;

                TLChatPhoto photo = chat.Photo as TLChatPhoto;

                MemoryStream inputStream = null;
                if (photo != null) {
                    TLFile file = await client.getProfilePic(photo.PhotoSmall as TLFileLocation);
                    inputStream = new MemoryStream(file.Bytes);

                }
                Group addGroup = new Group(name, id);
                addGroup.image = inputStream;
                addGroup.hasImage = inputStream != null;

                viewModel.addGroupToList(addGroup);
            }

            completedCallback();
        }


        protected override void OnAppearing() {
            base.OnAppearing();

            if (reloadListOnAppearing) {
                ItemsCollectionView.SelectedItem = null;
            } else { 
                reloadListOnAppearing = true; 
            }

            if (viewModel.groupList.Count == 0) {
                viewModel.IsBusy = true;
            }
        }

    }
}