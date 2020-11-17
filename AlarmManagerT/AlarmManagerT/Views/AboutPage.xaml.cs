﻿using AlarmManagerT.Models;
using AlarmManagerT.Resources;
using AlarmManagerT.Services;
using AlarmManagerT.ViewModels;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace AlarmManagerT.Views {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AboutPage : ContentPage {

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private AboutPageViewModel viewModel;

        public enum MESSAGING_KEYS { SHOW_ALERT_PAGE, RESTART_CLIENT}
        public AboutPage() {
            InitializeComponent();

            BindingContext = viewModel = new AboutPageViewModel();
            viewModel.requestRestartClient += restartClient;
            viewModel.requestShareLog += shareLog;
            viewModel.requestShowAlertPage += showAlertPage;
            viewModel.requestTestFCMMessage += testFCMMessage;
            viewModel.requestTestNotification += testNotification;
        }

        private void shareLog(object sender , EventArgs args) {
            Logger.Info("Launched Log sharing");
                        
            FileTarget target = NLog.LogManager.Configuration.FindTargetByName<FileTarget>("file");
            string logFile = target.FileName.Render(new NLog.LogEventInfo { TimeStamp = DateTime.Now });

            if (!File.Exists(logFile)) {
                Logger.Error("Log file could not be found.");
                return;
            }

            Interfaces.INavigation navigation = DependencyService.Get<Interfaces.INavigation>();
            navigation.navigateShare("LOG"); //TODO: Impelemnt this with actual NLOG.Log
        }

        private void showAlertPage(object sender, EventArgs args) {
            Logger.Info("User requested view AlertPage.");
            MessagingCenter.Send(this, MESSAGING_KEYS.SHOW_ALERT_PAGE.ToString());
        }

        private void restartClient(object sender, EventArgs args) {
            Logger.Info("Restarting client on user request.");
            MessagingCenter.Send(this, MESSAGING_KEYS.RESTART_CLIENT.ToString());
        }

        private void testFCMMessage(object sender, EventArgs args) {
            Logger.Info("Launching FCM Message Handling as if an external update was received in background.");
            new AlertService();
        }

        private void testNotification(object sender, EventArgs args) {
            Collection<string> configs = DataService.getConfigList();
            if(configs.Count > 0) {
                Logger.Info("Sending notification test message in 5s.");
                AlertConfig config = DataService.getAlertConfig(configs.First());

                Interfaces.INotifications notifications = DependencyService.Get<Interfaces.INotifications>();

                Task.Delay(5000).ContinueWith((t) => {
                    Logger.Info("Sending notification test message now.");
                    notifications.showAlertNotification(new Alert(AppResources.AboutPage_DeveloperMode_TestNotification_Message, config));
                });
            } else {
                Logger.Warn("Could not send notification test message as no alerts are configured.");
            }
        }
    }
}