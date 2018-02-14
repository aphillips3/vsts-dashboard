using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace VstsDashboard
{
    public class AsyncManager : IDisposable
    {
        private readonly List<BackgroundWorker> _workers = new List<BackgroundWorker>();

        public AsyncDoContext<T> Do<T>(Task<T> task) where T : class
        {
            if (!_workers.Any(w => w.IsBusy))
            {
                AsyncWorkBegun?.Invoke(this, EventArgs.Empty);
            }
            var backgroundWorker = new BackgroundWorker();
            _workers.Add(backgroundWorker);
            backgroundWorker.DoWork += (x, y) =>
            {
                task.Wait();
                y.Result = task.Result;
            };

            return new AsyncDoContext<T>(this, backgroundWorker);
        }

        public event EventHandler AsyncWorkBegun;
        public event EventHandler AsyncWorkComplete;

        public void Dispose()
        {
            foreach (var backgroundWorker in _workers)
            {
                backgroundWorker.Dispose();
            }
        }

        private void WorkerComplete()
        {
            if (!_workers.Any(w => w.IsBusy))
            {
                AsyncWorkComplete?.Invoke(this, EventArgs.Empty);
            }
        }

        public class AsyncDoContext<T> where T : class
        {
            private readonly AsyncManager _asyncManager;
            private readonly BackgroundWorker _worker;

            public AsyncDoContext(AsyncManager asyncManager, BackgroundWorker worker)
            {
                _asyncManager = asyncManager;
                _worker = worker;
            }

            public void Then(Action<T> continuation)
            {
                _worker.RunWorkerCompleted += (sender, args) =>
                {
                    _asyncManager.WorkerComplete();
                    _worker.Dispose();
                    continuation(args.Result as T);
                };
                _worker.RunWorkerAsync();
            }
        }
    }
}