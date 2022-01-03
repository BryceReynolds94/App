using BackgroundTasks;
using Foundation;
using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace PagerBuddy.iOS {
    class ServerRequestScheduler : IRequestScheduler {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static readonly string SERVER_REFRESH_TASK = "de.bartunik.pagerbuddy.serverRefresh";

        private CommunicationService client;
        public static ServerRequestScheduler instance;


        //TODO: iOS Implement BG Tasks
        //https://developer.apple.com/documentation/uikit/app_and_environment/scenes/preparing_your_ui_to_run_in_the_background/using_background_tasks_to_update_your_app

        public void initialise(CommunicationService client) {
            this.client = client;
            instance = this;
        }

        public void scheduleRequest(Collection<AlertConfig> request, string botServerUser) {
            
            BGProcessingTaskRequest req = new BGProcessingTaskRequest(SERVER_REFRESH_TASK);
            req.EarliestBeginDate = NSDate.Now.AddSeconds(15);
            req.RequiresNetworkConnectivity = true;
            
            bool res = BGTaskScheduler.Shared.Submit(req, out NSError error);
            if (!res) {
                Logger.Warn(error.DebugDescription, "Could not schedule background task.");
            }
        }

        public async Task runServerRefresh(BGTask task) {
            task.ExpirationHandler = new Action(() => {
                //Killed before completion
                task.SetTaskCompleted(false);
            });

            if(client != null) {
                await HandleStatus(client, client.clientStatus, task);
            } else {
                CommunicationService client = new CommunicationService();
                client.StatusChanged += async (object sender, CommunicationService.STATUS status) => {
                    await HandleStatus(client, status, task);
                };
            }
        }

        private async Task HandleStatus(CommunicationService client, CommunicationService.STATUS status, BGTask task) {

            if (status == CommunicationService.STATUS.AUTHORISED) {
                Collection<string> stringConfigList = DataService.getConfigList();
                Collection<AlertConfig> configList = new Collection<AlertConfig>();
                foreach (string configID in stringConfigList) {
                    AlertConfig config = DataService.getAlertConfig(configID, null);
                    if (config != null) {
                        configList.Add(config);
                    }
                }

                Logger.Debug("User authorised. Sending request.");
                bool success = await client.sendServerRequest(configList, CommunicationService.pagerbuddyServerList.First()); //TODO: Later implement multiple servers

                task.SetTaskCompleted(success);
            } else if (status > CommunicationService.STATUS.ONLINE) {
                //Wait status achieved - user is not authorised - do not bother in the future
                Logger.Warn("Server request not possible. User is not authorised. Status: " + status);
                task.SetTaskCompleted(true);
            } else if (status == CommunicationService.STATUS.OFFLINE) {
                //We do not have a connection, retry later...
                Logger.Debug("Client offline. Rescheduling server request for a later time.");
                task.SetTaskCompleted(false);
            }
        }
    }
}