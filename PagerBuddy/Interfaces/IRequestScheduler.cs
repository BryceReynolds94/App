using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace PagerBuddy.Interfaces {
    public interface IRequestScheduler {

        void scheduleRequest(Collection<AlertConfig> request, string botServerUser);

        void cancelRequest();

    }
}
