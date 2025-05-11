using System;
using System.Reflection;

namespace Ludo.Core.EventBus
{
    /// <summary>
    /// Internal subscription data for a single subscriber method.
    /// </summary>
    public class EventSubscription
    {
        public Type EventType;
        public object TargetInstance;
        public MethodInfo Method;
        public SubscriberPriority Priority;
        public bool IsDynamic;
        public Action<GameEvent> DynamicCallback;
    }
}