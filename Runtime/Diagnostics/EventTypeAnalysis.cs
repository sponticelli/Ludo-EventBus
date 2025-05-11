using System;

namespace Ludo.Core.EventBus.Diagnostics
{
    public class EventTypeAnalysis
    {
        public Type EventType { get; set; }
        public int Frequency { get; set; }
    }
}