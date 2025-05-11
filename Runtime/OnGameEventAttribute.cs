using System;

namespace Ludo.Core.EventBus
{
    /// <summary>
    /// Attribute for marking static subscriber methods.
    /// Example usage:
    /// [OnGameEvent(typeof(MyCustomEvent), SubscriberPriority.High)]
    /// private void HandleCustomEvent(MyCustomEvent evt) { ... }
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class OnGameEventAttribute : Attribute
    {
        public Type EventType { get; }
        public SubscriberPriority Priority { get; }

        public OnGameEventAttribute(Type eventType, SubscriberPriority priority = SubscriberPriority.High)
        {
            EventType = eventType;
            Priority = priority;
        }
    }
}