using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using AlarmManagerT.Models;
using AlarmManagerT.Services;

namespace AlarmManagerT.Views {
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : MasterDetailPage {
        public CommunicationService client;
        public MainPage() {
            InitializeComponent();

            MasterBehavior = MasterBehavior.Popover;
            client = new CommunicationService();

            Detail = new NavigationPage(new HomeStatusPage(client));
        }

        public async Task NavigateFromMenu(MenuPage.MENU_PAGE destination) {
            switch (destination) {
                case MenuPage.MENU_PAGE.AboutPage:
                    await requestAboutPageNavigation();
                    break;
                default:
                    break;
            }
        }

        private async Task requestAboutPageNavigation() {
            //TODO: Implement AboutPage navigation
            //await Detail.Navigation.PushAsync(new AboutPage());
            //IsPresented = false;
        }
    }
}