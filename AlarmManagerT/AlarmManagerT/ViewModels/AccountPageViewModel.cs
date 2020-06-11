using System;
using System.Collections.Generic;
using System.Text;
using TeleSharp.TL;
using Xamarin.Forms;

namespace AlarmManagerT.ViewModels
{
    public class AccountPageViewModel : BaseViewModel
    {
        public Command LogoutUser { get; set; }

        public AccountPageViewModel(string userName, string userPhone)
        {
            Title = "Account";
            updateInfo(userName, userPhone);

            LogoutUser = new Command(() => RequestLogout.Invoke(this, null));
        }

        public EventHandler RequestLogout;

        public void updateInfo(string userName, string userPhone)
        {
            ProfileName = userName;
            ProfilePhone = userPhone;
            OnPropertyChanged(nameof(ProfileName));
            OnPropertyChanged(nameof(ProfilePhone));
        }

        public void updatePhoto(bool hasPhoto, string photoLocation)
        {
            ProfilePicSource = photoLocation;
            ShowProfilePic = hasPhoto;
            OnPropertyChanged(nameof(ShowDefaultPic));
            OnPropertyChanged(nameof(ShowProfilePic));
            OnPropertyChanged(nameof(ProfilePicSource));
        }

        public bool ShowDefaultPic => !ShowProfilePic;
        public bool ShowProfilePic { get; private set; } = false;
        public string ProfilePicSource { get; private set; } = null;
        public string ProfileName { get; private set; }
        public string ProfilePhone { get; private set; }


    }
}
