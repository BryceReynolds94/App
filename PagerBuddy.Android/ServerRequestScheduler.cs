using Android.App;
using Android.App.Job;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

[assembly: Xamarin.Forms.Dependency(typeof(PagerBuddy.Droid.ServerRequestScheduler))] //register for dependency service as platform-specific code
namespace PagerBuddy.Droid {
    class ServerRequestScheduler : IRequestScheduler {
        private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly int SERVER_REQUEST_ID = 1;
        public enum JOB_PARAMETERS { REQUEST_STRING, PAGERBUDDY_SERVER_USER }

        private readonly JobScheduler jobScheduler = (JobScheduler)Application.Context.GetSystemService(Context.JobSchedulerService);

        public static ServerRequestScheduler instance;
        public CommunicationService client;

        public void initialise(CommunicationService client) {
            instance = this;
            this.client = client;
        }

        public void scheduleRequest(Collection<AlertConfig> request, string botServerUser) {
            Logger.Debug("Scheduling a server request.");
            ComponentName componentName = new ComponentName(Application.Context, Java.Lang.Class.FromType(typeof(ServerRequestService)));
            JobInfo.Builder builder = new JobInfo.Builder(SERVER_REQUEST_ID, componentName);
            builder.SetBackoffCriteria(1 * 60 * 1000, BackoffPolicy.Linear); //Initially set for 1min, use linear back off (capped at 5h by Android)
            builder.SetMinimumLatency(5 * 1000); //Initially wait 5s to reduce flooding
            builder.SetPersisted(true); //Do not loose service on reboot -- need RECEIVE_BOOT_COMPLETED permission

            if (Build.VERSION.SdkInt >= BuildVersionCodes.P) {
                builder.SetEstimatedNetworkBytes(JobInfo.NetworkBytesUnknown, JobInfo.NetworkBytesUnknown);
                builder.SetRequiredNetwork(new NetworkRequest.Builder().AddCapability(NetCapability.Internet).Build());
            } else {
                builder.SetRequiredNetworkType(NetworkType.Any);
            }

            PersistableBundle jobParameters = new PersistableBundle();
            jobParameters.PutString(nameof(JOB_PARAMETERS.REQUEST_STRING), JsonConvert.SerializeObject(request));
            jobParameters.PutString(nameof(JOB_PARAMETERS.PAGERBUDDY_SERVER_USER), botServerUser);

            builder.SetExtras(jobParameters);

            int scheduleResult = jobScheduler.Schedule(builder.Build());
            if (scheduleResult != JobScheduler.ResultSuccess) {
                Logger.Error("Scheduling a server update job failed.");
            }
        }

        public void cancelRequest() {
            Logger.Debug("Cancelling server request if active.");
            jobScheduler.Cancel(SERVER_REQUEST_ID);
        }

    }
}