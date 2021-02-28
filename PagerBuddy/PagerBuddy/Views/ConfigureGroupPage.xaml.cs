using LanguageExt;
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

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Types = Telega.Rpc.Dto.Types;

namespace PagerBuddy.Views {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfigureGroupPage : ContentPage {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ConfigureGroupPageViewModel viewModel;
        private readonly AlertConfig alertConfig;
        private readonly CommunicationService client;

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

            Types.Messages.Dialogs rawChatList = await client.getChatList();

            Arr<Types.Dialog> dialogList = new Arr<Types.Dialog>();
            Collection<TelegramPeer> peerCollection = new Collection<TelegramPeer>();

            if (rawChatList == null) {
                viewModel.IsBusy = false;
                Logger.Warn("Retrieving chat list returned null. Returning to HomePage.");
                await Navigation.PopAsync();
                completedCallback();
                return;
            } else if (rawChatList.AsTag().IsSome) {
                Types.Messages.Dialogs.Tag dialogsTag = rawChatList.AsTag().Single();
                dialogList = dialogsTag.Dialogs;
                peerCollection = TelegramPeer.getPeerCollection(dialogsTag.Chats, dialogsTag.Users);
            } else if (rawChatList.AsSliceTag().IsSome) {
                Logger.Info("Return type was DialogsSlice. Presumably user has more than 100 active dialogs.");
                Types.Messages.Dialogs.SliceTag dialogsTag = rawChatList.AsSliceTag().Single();
                dialogList = dialogsTag.Dialogs;
                peerCollection = TelegramPeer.getPeerCollection(dialogsTag.Chats, dialogsTag.Users);
            } else if (rawChatList.AsNotModifiedTag().IsSome) {
                Logger.Warn("Return type was DialogsNotModified. This case is not implemented and will be treated as empty chat list.");
            }

            if (dialogList.Count < 1) {
                viewModel.IsBusy = false;
                Logger.Warn("Chat list was empty.");
                viewModel.AreChatsEmpty = true;
                completedCallback();
                return;
            }
            viewModel.AreChatsEmpty = false;

            foreach (Types.Dialog dialog in dialogList) {
                if (dialog.AsTag().IsNone) {
                    Logger.Info("Dialog list contained empty member.");
                    continue;
                }

                Types.Peer peer = dialog.AsTag().Single().Peer;
                int id;

                if (peer.AsChannelTag().IsSome) {
                    id = peer.AsChannelTag().Single().ChannelId;
                } else if (peer.AsChatTag().IsSome) {
                    id = peer.AsChatTag().Single().ChatId;
                } else if (peer.AsUserTag().IsSome) {
                    id = peer.AsUserTag().Single().UserId;
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
                    _ = detailPeer.loadImage(client); //Do not wait for image laoding to avoid blocking
                }

                viewModel.addGroupToList((Group)detailPeer);
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