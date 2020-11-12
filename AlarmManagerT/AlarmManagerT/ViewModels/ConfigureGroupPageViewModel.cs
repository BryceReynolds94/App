using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

using Xamarin.Forms;

using AlarmManagerT.Models;
using AlarmManagerT.Views;
using AlarmManagerT.Services;
using TeleSharp.TL;
using TeleSharp.TL.Upload;
using System.IO;
using AlarmManagerT.Resources;

namespace AlarmManagerT.ViewModels
{
    public class ConfigureGroupPageViewModel : BaseViewModel
    {
        public ObservableCollection<GroupViewModel> groupList { get; set; }
        public Command LoadItemsCommand { get; set; }

        public Command GroupSelectionChangedCommand { get; set; }
        public GroupViewModel SelectedGroup { get; set; }

        public ConfigureGroupPageViewModel()
        {

            Title = AppResources.ConfigureGroupPage_Title;
            groupList = new ObservableCollection<GroupViewModel>();
            
            LoadItemsCommand = new Command(() => ExecuteLoadItemsCommand());
            GroupSelectionChangedCommand = new Command(() => GroupSelected());


        }

        public GroupSelectionHandler GroupSelectionMade;
        public delegate void GroupSelectionHandler(object sender, Group selectedGroup);

        private void GroupSelected()
        {
            if(SelectedGroup == null)
            {
                //TODO: Something went wrong - handle
                return;
            }
            GroupSelectionMade.Invoke(this, SelectedGroup.group);
        }

        public void clearGroupList()
        {
            groupList.Clear();
        }

        public void addGroupToList(Group addGroup)
        {
            GroupViewModel groupViewModel = new GroupViewModel(addGroup);
            groupList.Add(groupViewModel);
        }

        public ReloadGroupHandler ReloadGroupList;
        public delegate void ReloadGroupHandler(object sender, Action reloadCompleted);

        private void ExecuteLoadItemsCommand()
        {
            IsBusy = true;
            Action callback = new Action(() => IsBusy = false);
            
            ReloadGroupList.Invoke(this, callback);
        }
    }
}