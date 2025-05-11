namespace Ludo.Core.EventBus
{
    /// <summary>
    /// Event priority levels. 'Essential' and 'Cleanup' are processed regardless of cancellation/filter.
    /// </summary>
    public enum SubscriberPriority
    {
        Essential = 0, // Always executed first, even if canceled.
        High = 1, // Default for static subscribers.
        Medium = 2, // Default for dynamic subscribers.
        Low = 3, // Final standard level.
        Cleanup = 4 // Always executed last, even if canceled.
    }
}