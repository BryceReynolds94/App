using Android.App;
using Android.App.Job;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using PagerBuddy.Models;
using PagerBuddy.Services;
using PagerBuddy.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagerBuddy.Droid {

    [Service(Name = "de.bartunik.pagerbuddy.serverrequestservice", Permission = "android.permission.BIND_JOB_SERVICE")]
    class ServerRequestService : JobService {
        private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public override bool OnStartJob(JobParameters jobParams) {
            //https://docs.microsoft.com/en-us/xamarin/android/platform/android-job-scheduler

            if(ServerRequestScheduler.instance != null && ServerRequestScheduler.instance.client != null) { //Retry with current instance if live
                Logger.Debug("Client active in foreground. Reusing instance for attempting repeated server request.");
                _ = HandleStatus(ServerRequestScheduler.instance.client, ServerRequestScheduler.instance.client.clientStatus, jobParams);
            } else {
                CommunicationService client = new CommunicationService();
                client.StatusChanged += async (object sender, CommunicationService.STATUS status) => {
                    await HandleStatus(client, status, jobParams);
                };
                _ = client.connectClient(true);
            }

            return true;
        }

        private async Task HandleStatus(CommunicationService client, CommunicationService.STATUS status, JobParameters jobParams) {

            string requestString = jobParams.Extras.GetString(nameof(ServerRequestScheduler.JOB_PARAMETERS.REQUEST_STRING));
            Collection<AlertConfig> request = JsonConvert.DeserializeObject<Collection<AlertConfig>>(requestString);
            string serverUser = jobParams.Extras.GetString(nameof(ServerRequestScheduler.JOB_PARAMETERS.PAGERBUDDY_SERVER_USER));

            if (status == CommunicationService.STATUS.AUTHORISED) {
                Logger.Debug("User authorised. Sending request.");
                bool success = await client.sendServerRequest(request, serverUser);

                JobFinished(jobParams, !success); //Do not reschedule on success
            } else if (status > CommunicationService.STATUS.ONLINE) {
                //Wait status achieved - user is not authorised - do not bother in the future
                Logger.Warn("Server request not possible. User is not authorised. Status: " + status);
                JobFinished(jobParams, false);
            } else if (status == CommunicationService.STATUS.OFFLINE) {
                //We do not have a connection retry later...
                Logger.Debug("Client offline. Rescheduling server request for a later time.");
                JobFinished(jobParams, true);
            }
        }

        public override bool OnStopJob(JobParameters @params) {
            Logger.Warn("Repeat server request job killed before completion.");
            //Job wll be killed before completion.

            //Request reschedule
            return true;
        }
    }
}