using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using AlarmManagerT.Services;
using AlarmManagerT.Views;
using Plugin.FirebasePushNotification;
using System.Collections;

namespace AlarmManagerT
{
    public partial class App : Application
    {

        public App()
        {
            //TODO: Keep an eye on the experimental flags
            Device.SetFlags(new string[] { "RadioButton_Experimental" });

            InitializeComponent();

            //DependencyService.Register<MockDataStore>();
            MainPage = new MainPage();

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
