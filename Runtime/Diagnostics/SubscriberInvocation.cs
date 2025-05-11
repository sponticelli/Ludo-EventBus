using System;

namespace Ludo.Core.EventBus.Diagnostics
{
    public class SubscriberInvocation
    {
        public string SubscriberName { get; set; }
        public SubscriberPriority Priority { get; set; }
        public long DurationMs { get; set; }
        public bool CanceledEvent { get; set; }
        public Exception Error { get; set; }
    }
}