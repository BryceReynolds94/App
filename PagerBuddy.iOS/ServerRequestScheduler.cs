using Foundation;
using PagerBuddy.Interfaces;
using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UIKit;

namespace PagerBuddy.iOS {
    class ServerRequestScheduler : IRequestScheduler {

        //TODO: Implement BG Tasks
        //https://developer.apple.com/documentation/uikit/app_and_environment/scenes/preparing_your_ui_to_run_in_the_background/using_background_tasks_to_update_your_app
        public void cancelRequest() {
            throw new NotImplementedException();
        }

        public void scheduleRequest(Collection<AlertConfig> request, string botServerUser) {
            throw new NotImplementedException();
        }
    }
}