using AlarmManagerT.Resources;
using AlarmManagerT.Services;
using AlarmManagerT.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleSharp.TL;
using TeleSharp.TL.Upload;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace AlarmManagerT.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AccountPage : ContentPage
    {

        private AccountPageViewModel viewModel;
        public AccountPage(MyClient client)
        {
            InitializeComponent();

            string userName = Data.getConfigValue(Data.DATA_KEYS.USER_NAME, AppResources.AccountPage_UserName_Default);
            string userPhone = Data.getConfigValue(Data.DATA_KEYS.USER_PHONE, AppResources.AccountPage_UserPhone_Default);

            BindingContext = viewModel = new AccountPageViewModel(userName, userPhone);
            viewModel.RequestLogout += logoutUser;

            MessagingCenter.Subscribe<MyClient>(this, "UserDataChanged", (obj) => updateUserView());

            updateUser(client);
        }

        private void logoutUser(object sender, EventArgs e)
        {
            //TODO: Implement this
            //TODO: Clear all configurations on logout
            //TODO: Return to login áfter logout
            return;
        }

        private async void updateUser(MyClient client)
        {
            TLUser user = await client.getUser();
            client.saveUserData(user);
        }

        private void updateUserView()
        {
            bool hasPhoto = Data.getConfigValue(Data.DATA_KEYS.USER_HAS_PHOTO, false);
            string photoLocation = null;
            if (hasPhoto)
            {
                photoLocation = Data.profilePicSavePath(Data.DATA_KEYS.USER_PHOTO.ToString());
            }
            viewModel.updatePhoto(hasPhoto, photoLocation);

            string userName = Data.getConfigValue(Data.DATA_KEYS.USER_NAME, AppResources.AccountPage_UserName_Default);
            string userPhone = Data.getConfigValue(Data.DATA_KEYS.USER_PHONE, AppResources.AccountPage_UserPhone_Default);

            viewModel.updateInfo(userName, userPhone);
        }


    }
}