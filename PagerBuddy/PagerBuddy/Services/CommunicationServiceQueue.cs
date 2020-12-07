using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PagerBuddy.Services {
    public class CommunicationServiceQueue {

        private SemaphoreSlim semaphore;

        public CommunicationServiceQueue() {
            semaphore  = new SemaphoreSlim(1);
        }

        public async Task<T> Enqueue<T>(Func<Task<T>> task) {
            await semaphore.WaitAsync();
            try {
                return await task();
            } finally {
                semaphore.Release();
            }
        }

        public async Task Enqueue(Func<Task> task) {
            await semaphore.WaitAsync();
            try {
                await task();
            } finally {
                semaphore.Release();
            }
        }
    }
}
