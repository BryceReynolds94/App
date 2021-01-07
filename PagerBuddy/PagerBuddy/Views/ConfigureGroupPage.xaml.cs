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
using TeleSharp.TL.Messages;
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

            TLAbsDialogs rawChatList = await client.getChatList();

            TLVector<TLDialog> dialogList;
            Collection<TelegramPeer> peerCollection;

            if (rawChatList is TLDialogs) {
                TLDialogs dialogs = rawChatList as TLDialogs;
                dialogList = dialogs.Dialogs;
                peerCollection = TelegramPeer.getPeerCollection(dialogs.Chats, dialogs.Users);
            } else if (rawChatList is TLDialogsSlice) {
                Logger.Info("Return type was TLDialogsSlice. Presumably user has more than 100 active dialogs.");
                TLDialogsSlice dialogs = rawChatList as TLDialogsSlice;
                dialogList = dialogs.Dialogs;
                peerCollection = TelegramPeer.getPeerCollection(dialogs.Chats, dialogs.Users);
            } else {
                Logger.Warn("Unexpected return type while retrieving chatList. Type: " + rawChatList.GetType().ToString());
                viewModel.IsBusy = false;
                completedCallback();
                return;
            }

            if (dialogList.Count < 1) {
                viewModel.IsBusy = false;
                Logger.Warn("Retrieving chat list returned no result.");

                //check is client still available
                if (client.clientStatus != CommunicationService.STATUS.AUTHORISED) {
                    Logger.Info("Client is not authorised. Returning to HomePage. Client status: " + client.clientStatus.ToString());
                    await Navigation.PopAsync();
                } else {
                    viewModel.AreChatsEmpty = true;
                }
                completedCallback();
                return;
            }
            viewModel.AreChatsEmpty = false;

            foreach (TLDialog dialog in dialogList) {
                TLAbsPeer peer = dialog.Peer;
                int id;

                if (peer is TLPeerChannel) {
                    id = (peer as TLPeerChannel).ChannelId;
                } else if (peer is TLPeerChat) {
                    id = (peer as TLPeerChat).ChatId;
                } else if (peer is TLPeerUser) {
                    id = (peer as TLPeerUser).UserId;
                } else {
                    Logger.Warn("Peer was of unexpected type " + peer.GetType().ToString() + " and will be ignored.");
                    continue;
                }

                TelegramPeer detailPeer = peerCollection.FirstOrDefault((TelegramPeer x) => x.id == id);
                if (detailPeer == null || detailPeer.id != id) {
                    Logger.Info("Details for a peer could not be found in peer collection and will be ignored.");
                    continue;
                }

                if (detailPeer.photoLocation != null) {
                    TLFile file = await client.getProfilePic(detailPeer.photoLocation);
                    if (file != null) {
                        detailPeer.image = new MemoryStream(file.Bytes);
                        detailPeer.hasImage = detailPeer.image != null;
                    } else {
                        Logger.Info("Could not load peer pic.");
                    }
                }

                viewModel.addGroupToList((Group) detailPeer);
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