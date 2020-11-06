using AlarmManagerT.Services;
using AlarmManagerT.Views;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace AlarmManagerT.ViewModels
{
    class MenuPageViewModel : BaseViewModel
    {
        public Command ViewAccount { get; set; }
        public Command Test { get; set; } //TODO: RBF

        public MenuPageViewModel()
        {
            ViewAccount = new Command(() => RequestNavigation.Invoke(this, MenuPage.MENU_PAGE.AccountPage));
            Test = new Command(() => MessagingCenter.Send(this, "TEST")); //TODO: RBF

            MessagingCenter.Subscribe<MyClient>(this, "UserDataChanged", (obj) => userDataChanged());
        }

        public NavigationEventHandler RequestNavigation;
        public delegate void NavigationEventHandler(object sender, MenuPage.MENU_PAGE destination);

        private void userDataChanged()
        {
            OnPropertyChanged(nameof(UserName));
            OnPropertyChanged(nameof(UserPhone));
            OnPropertyChanged(nameof(UserHasPic));
            OnPropertyChanged(nameof(ShowDefaultPic));
            OnPropertyChanged(nameof(UserPic));
        }

        public string UserName => Data.getConfigValue(Data.DATA_KEYS.USER_NAME, "Username");
        public string UserPhone => Data.getConfigValue(Data.DATA_KEYS.USER_PHONE, "Phone Number");
        public bool UserHasPic => Data.getConfigValue(Data.DATA_KEYS.USER_HAS_PHOTO, false);

        public bool ShowDefaultPic => !UserHasPic;
        public string UserPic {
            get {
                if (UserHasPic)
                {
                    return Data.profilePicSavePath(Data.DATA_KEYS.USER_PHOTO.ToString());
                }
                else
                {
                    return null;
                }
        }
        }
    }
}
