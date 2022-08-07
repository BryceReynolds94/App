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
using System.Threading;
using System.Threading.Tasks;
using UIKit;

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.iOS.ServerRequestScheduler))] //register for dependency service as platform-specific code
namespace PagerBuddy.iOS {
    class ServerRequestScheduler : IRequestScheduler {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static readonly string SERVER_REFRESH_TASK = "de.bartunik.pagerbuddy.serverRefresh"; //Caution! Has to be identical in info.plist

        private CommunicationService client;
        public static ServerRequestScheduler instance;

        private static CancellationTokenSource cancellationSource;

        //https://developer.apple.com/documentation/uikit/app_and_environment/scenes/preparing_your_ui_to_run_in_the_background/using_background_tasks_to_update_your_app

        public void initialise(CommunicationService client) {
            this.client = client;
            instance = this;
        }

        public async Task backgroundRequest(BGTask task) {

            

            CancellationTokenSource bgCancellationSource = new CancellationTokenSource();

            task.ExpirationHandler += () => {
                bgCancellationSource.Cancel();
                task.SetTaskCompleted(false);
            };

            Collection<string> stringConfigList = DataService.getConfigList();
            Collection<AlertConfig> configList = new Collection<AlertConfig>();
            foreach (string configID in stringConfigList) {
                AlertConfig config = DataService.getAlertConfig(configID, null);
                if (config != null) {
                    configList.Add(config);
                }
            }

            bool result = await runServerRefresh(client, configList, bgCancellationSource.Token);
            task.SetTaskCompleted(result);
        }

        public void scheduleRequest(Collection<AlertConfig> request) {
            _ = scheduleRequest(request, 2);
        }

        private async Task scheduleRequest(Collection<AlertConfig> request, int delay) {

            cancellationSource?.Cancel();
            cancellationSource?.Dispose();

            cancellationSource = new CancellationTokenSource();


            await Task.Run(async () => {

                nint taskID = UIApplication.SharedApplication.BeginBackgroundTask(() => {
                    //Task ending before completion
                    Logger.Info("Could not complete server request in alotted background time. Rescheduling.");

                    BGAppRefreshTaskRequest req = new BGAppRefreshTaskRequest(SERVER_REFRESH_TASK);
                    req.EarliestBeginDate = NSDate.Now.AddSeconds(5); //Reset backoff when commiting task to background

                    bool res = BGTaskScheduler.Shared.Submit(req, out NSError error);
                    if (!res) {
                        Logger.Warn(error.DebugDescription, "Could not schedule background task.");
                    }

                    cancellationSource.Cancel();
                });

                cancellationSource.Token.Register(() => {
                    UIApplication.SharedApplication.EndBackgroundTask(taskID);
                });

                try {
                    await Task.Delay(delay * 1000, cancellationSource.Token);
                } catch (TaskCanceledException) {
                    return;
                }

                bool success = await runServerRefresh(client, request, cancellationSource.Token);
                if (!success && !cancellationSource.IsCancellationRequested) {
                    int newDelay = delay + 60; //Linear back off, cap at 5h
                    newDelay = newDelay < 5 * 60 * 60 ? newDelay : 5 * 60 * 60;
                    _ = scheduleRequest(request, newDelay);
                }

                cancellationSource.Cancel();
            });

        }

        private async Task<bool> runServerRefresh(CommunicationService client, Collection<AlertConfig> configList, CancellationToken cancellationToken) {
            if(client == null) {
                client = new CommunicationService();
                _ = client.connectClient(true);
            }

            if (client.clientStatus < CommunicationService.STATUS.WAIT_PHONE && client.clientStatus != CommunicationService.STATUS.OFFLINE) {

                Task<Task<bool>> handleTask = new Task<Task<bool>>(async () => await HandleStatus(client, configList));
                CommunicationService.ClientStausEventHandler handler = (object sender, CommunicationService.STATUS status) => {
                    if (status < CommunicationService.STATUS.WAIT_PHONE && status != CommunicationService.STATUS.OFFLINE) {
                        return;
                    }
                    handleTask.Start();
                };

                client.StatusChanged += handler;

                Task<bool> cancelTask = new Task<bool>(() => false);
                cancellationToken.Register(() => {
                    client.StatusChanged -= handler;
                    cancelTask.Start();
                });

                return await await Task.WhenAny(await handleTask, cancelTask);
            } else {
                return await HandleStatus(client, configList);
            }
        }

        private async Task<bool> HandleStatus(CommunicationService client, Collection<AlertConfig> configList) {

            CommunicationService.STATUS status = client.clientStatus;
            if (status == CommunicationService.STATUS.AUTHORISED) {
                Logger.Debug("User authorised. Sending request.");
                bool success = await client.sendServerRequests(configList);
                return success;
            } else if (status > CommunicationService.STATUS.ONLINE) {
                //Wait status achieved - user is not authorised - do not bother in the future
                Logger.Warn("Server request not possible. User is not authorised. Status: " + status);
                return true;
            } else{
                //We do not have a connection, retry later...
                Logger.Debug("Client offline. Rescheduling server request for a later time.");
                return false;
            }
        }
    }
}