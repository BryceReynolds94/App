using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using AlarmManagerT.Services;
using AlarmManagerT.Views;
using Plugin.FirebasePushNotification;
using System.Collections;
using AlarmManagerT.ViewModels;

namespace AlarmManagerT
{
    public partial class App : Application
    {

        public App()
        {
            //TODO: Keep an eye on the experimental flags
            Device.SetFlags(new string[] { "RadioButton_Experimental" });

            InitializeComponent();

            new AlertHandler();
            //DependencyService.Register<MockDataStore>();
            MainPage = new MainPage();

            MessagingCenter.Subscribe<MenuPageViewModel>(this, "TEST", (_) => testPoint(((MainPage) Current.MainPage).client)); //TODO: RBF

        }

        private void testPoint(MyClient client) //TODO: RBF
        {
            AlertHandler handler = new AlertHandler();
            handler.showUserNotification("Manual Test");

            client.subscribePushNotifications(CrossFirebasePushNotification.Current.Token); //TODO: RBF
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
