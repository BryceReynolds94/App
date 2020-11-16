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
using AlarmManagerT.Models;

namespace AlarmManagerT
{
    //TODO: Refactoring: Move Images folder into Resources folder -- all XML usage has to be refactored
    public partial class App : Application
    {
        public App(bool isAlert, Alert alert)
        {
            initialise(isAlert, alert);
        }

        public App()
        {
            initialise(false);
        }

        private void initialise(bool isAlert, Alert alert = null)
        {
            //TODO: Keep an eye on the experimental flags
            Device.SetFlags(new string[] { "RadioButton_Experimental" });

            InitializeComponent();

            MessagingCenter.Subscribe<MenuPageViewModel>(this, "TEST", (_) => testPoint(((MainPage)Current.MainPage).client)); //TODO: RBF

            if (isAlert)
            {
                MainPage = new AlertPage(alert);
                return;
            }

            MainPage = new MainPage();
            new MessagingService().SetupListeners(((MainPage)Current.MainPage).client);
        }

        private void testPoint(CommunicationService client) //TODO: RBF
        {
            //client.subscribePushNotifications(CrossFirebasePushNotification.Current.Token); //TODO: RBF

            /*Task.Delay(2000).ContinueWith(t =>
            {
                INotifications notifications = DependencyService.Get<INotifications>();
                notifications.showAlertNotification(Alert.getTestSample("56561564544")); //TODO: RBF
            });*/
            MainPage = new AlertPage(Alert.getTestSample("32156464864"));
            return;
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
