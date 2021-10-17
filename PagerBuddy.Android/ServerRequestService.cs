using Android.App;
using Android.App.Job;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PagerBuddy.Droid {
    class ServerRequestScheduler {
        NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly int SERVER_REQUEST_ID = 1;
        public enum JOB_PARAMETERS { REQUEST_STRING, PAGERBUDDY_SERVER_USER}

        private JobScheduler jobScheduler = (JobScheduler)Application.Context.GetSystemService(Context.JobSchedulerService);

        public void scheduleRequest(string request, string botServerUser) {
            ComponentName componentName = new ComponentName(Application.Context, Java.Lang.Class.FromType(typeof(ServerRequestService)));
            JobInfo.Builder builder = new JobInfo.Builder(SERVER_REQUEST_ID, componentName);
            builder.SetBackoffCriteria(5 * 60 * 1000, BackoffPolicy.Exponential); //Initially set fro 5min, use exponential back off
            builder.SetMinimumLatency(5 * 60 * 1000);
            builder.SetPersisted(true); //Do not loose service on reboot -- need RECEIVE_BOOT_COMPLETED permission

            if (Build.VERSION.SdkInt >= BuildVersionCodes.P) {
                builder.SetEstimatedNetworkBytes(JobInfo.NetworkBytesUnknown, JobInfo.NetworkBytesUnknown);
                builder.SetRequiredNetwork(new NetworkRequest.Builder().AddCapability(NetCapability.Internet).Build());
            } else {
                builder.SetRequiredNetworkType(NetworkType.Any);
            }

            PersistableBundle jobParameters = new PersistableBundle();
            jobParameters.PutString(nameof(JOB_PARAMETERS.REQUEST_STRING), request);
            jobParameters.PutString(nameof(JOB_PARAMETERS.PAGERBUDDY_SERVER_USER), botServerUser);

            builder.SetExtras(jobParameters);

            int scheduleResult = jobScheduler.Schedule(builder.Build());
            if(scheduleResult != JobScheduler.ResultSuccess) {
                Logger.Error("Scheduling a server update job failed.");
            }
        }

        public void cancelRequest() {
            jobScheduler.Cancel(SERVER_REQUEST_ID);
        }
    }

    [Service(Name = "de.bartunik.pagerbuddy.serverrequestservice", Permission = "android.permission.BIND_JOB_SERVICE")]
    class ServerRequestService : JobService {
        public override bool OnStartJob(JobParameters jobParams) {
            //https://docs.microsoft.com/en-us/xamarin/android/platform/android-job-scheduler

            string requestString = jobParams.Extras.GetString(nameof(ServerRequestScheduler.JOB_PARAMETERS.REQUEST_STRING));
            string serverUser = jobParams.Extras.GetString(nameof(ServerRequestScheduler.JOB_PARAMETERS.PAGERBUDDY_SERVER_USER));

            CommunicationService client = new CommunicationService(true);
            client.StatusChanged += (object sender, CommunicationService.STATUS status) => {
                if(status == CommunicationService.STATUS.AUTHORISED) {
                    //TODO: send request here;

                    JobFinished(jobParams, false); //Do not reschedule on success
                }
            };

            return true;

        }

        public override bool OnStopJob(JobParameters @params) {
            return true;
        }
    }
}