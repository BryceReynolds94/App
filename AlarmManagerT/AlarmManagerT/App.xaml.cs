using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using AlarmManagerT.Services;
using AlarmManagerT.Views;
using System.Collections;
using AlarmManagerT.ViewModels;
using Plugin.FirebasePushNotification;

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

            new FirebaseMessagingHandler().SetupListeners(((MainPage) Current.MainPage).client);

            MessagingCenter.Subscribe<MenuPageViewModel>(this, "TEST", (_) => testPoint(((MainPage) Current.MainPage).client)); //TODO: RBF

        }

        private void testPoint(MyClient client) //TODO: RBF
        {
            //client.subscribePushNotifications(CrossFirebasePushNotification.Current.Token); //TODO: RBF

            NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();

            NLog.Config.LoggingConfiguration config = NLog.LogManager.Configuration;

            Logger.Warn("Test");
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
