using System;
using System.Collections.Generic;

namespace Ludo.Core.EventBus.Diagnostics
{
    public class EventTrace
    {
        public string EventId { get; set; }
        public Type EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public List<SubscriberInvocation> Invocations { get; } = new List<SubscriberInvocation>();
        public long TotalDurationMs { get; set; }
        public bool WasCanceled { get; set; }
        public string PublisherStackTrace { get; set; }
    }
}