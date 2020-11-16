using AlarmManagerT.Models;
using AlarmManagerT.Services;
using AlarmManagerT.ViewModels;
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

namespace AlarmManagerT.Views {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfigureGroupPage : ContentPage {
        ConfigureGroupPageViewModel viewModel;

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
                addGroup.lastMessageID = await client.getCurrentMessageID(id);

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