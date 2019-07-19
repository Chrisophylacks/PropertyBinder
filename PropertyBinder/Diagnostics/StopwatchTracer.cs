using System;
using System.Diagnostics;

namespace PropertyBinder.Diagnostics
{
    public sealed class StopwatchTracer : IBindingTracer
    {
        private readonly Stopwatch stopwatch = new Stopwatch();

        public void Reset()
        {
            stopwatch.Reset();
        }

        public TimeSpan Elapsed => stopwatch.Elapsed;

        public void OnScheduled(string bindingDescription)
        {
        }

        public void OnIgnored(string bindingDescription)
        {
        }

        public void OnStarted(string bindingDescription)
        {
            stopwatch.Start();
        }

        public void OnEnded(string bindingDescription)
        {
            stopwatch.Stop();
        }

        public void OnException(Exception ex)
        {
            if (stopwatch.IsRunning)
            {
                OnEnded(string.Empty);
            }
        }
    }
}