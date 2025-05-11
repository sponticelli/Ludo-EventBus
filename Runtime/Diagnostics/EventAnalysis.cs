namespace Ludo.Core.EventBus.Diagnostics
{
    public class EventAnalysis
    {
        public string Name { get; set; }
        public double AverageDurationMs { get; set; }
        public long MaxDurationMs { get; set; }
        public int InvocationCount { get; set; }
    }
}