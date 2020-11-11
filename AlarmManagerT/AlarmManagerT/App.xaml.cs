using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using AlarmManagerT.Services;
using AlarmManagerT.Views;
using System.Collections;
using AlarmManagerT.ViewModels;
using Plugin.FirebasePushNotification;
using AlarmManagerT.Interfaces;
using System.Threading.Tasks;

namespace AlarmManagerT
{
    public partial class App : Application
    {
        public App(bool isAlert)
        {
            if (!isAlert)
            {
                initialise(isAlert);
                return;
            }
        }

        public App()
        {
            initialise(false);
        }

        private void initialise(bool isAlert)
        {
            //TODO: Keep an eye on the experimental flags
            Device.SetFlags(new string[] { "RadioButton_Experimental" });

            InitializeComponent();

            new AlertHandler();
            //DependencyService.Register<MockDataStore>();

            if (isAlert)
            {
                MainPage = new AlertPage();
                return;
            }

            MainPage = new MainPage();

            new FirebaseMessagingHandler().SetupListeners(((MainPage)Current.MainPage).client);

            MessagingCenter.Subscribe<MenuPageViewModel>(this, "TEST", (_) => testPoint(((MainPage)Current.MainPage).client)); //TODO: RBF
        }

        private void testPoint(MyClient client) //TODO: RBF
        {
            //client.subscribePushNotifications(CrossFirebasePushNotification.Current.Token); //TODO: RBF

            Task.Delay(2000).ContinueWith(t =>
            {
                INotifications notifications = DependencyService.Get<INotifications>();
                notifications.showAlertNotification("TESTPOINT Notification", "56561564544"); //TODO: RBF
            });

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
