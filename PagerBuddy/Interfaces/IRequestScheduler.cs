using PagerBuddy.Models;
using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace PagerBuddy.Interfaces {
    public interface IRequestScheduler {
        void initialise(CommunicationService client);
        void scheduleRequest(Collection<AlertConfig> request);

    }
}
