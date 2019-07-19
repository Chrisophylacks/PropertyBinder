using System;

namespace PropertyBinder.Diagnostics
{
    public interface IBindingTracer
    {
        void OnScheduled(string bindingDescription);

        void OnIgnored(string bindingDescription);

        void OnStarted(string bindingDescription);

        void OnEnded(string bindingDescription);

        void OnException(Exception ex);
    }
}
