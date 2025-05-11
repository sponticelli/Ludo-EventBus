# Ludo EventBus - API Documentation

This document provides detailed information about the core interfaces and classes in the Ludo EventBus system.

## Core Classes

### GameEvent

The base class for all events in the system.

```csharp
namespace Ludo.Core.EventBus
{
    public abstract class GameEvent
    {
        // Indicates whether this event has been canceled by a subscriber
        public bool IsCanceled { get; private set; }

        // Call this to prevent further subscribers (of lower priority) from handling the event
        public void StopPropagation()
        {
            IsCanceled = true;
        }
    }
}
```

**Usage:**
- Inherit from this class to create custom events
- Use `StopPropagation()` to cancel event propagation

### GameEventHub

The central hub for publishing events and managing subscriptions.

```csharp
namespace Ludo.Core.EventBus
{
    public static class GameEventHub
    {
        // Binds all methods in the 'subscriber' that have [OnGameEvent]
        public static void Bind(object subscriber);

        // Unbinds all methods in the 'subscriber' that were registered via Bind()
        public static void Unbind(object subscriber);

        // Subscribes a single callback method to a specific event type
        // Returns an Action you should store and call to unsubscribe
        public static Action Listen<TEvent>(
            object subscriber,
            Action<TEvent> callback,
            SubscriberPriority priority = SubscriberPriority.Medium)
            where TEvent : GameEvent;

        // Publishes the event to all subscribers, in priority order
        public static void Publish(GameEvent evt);
    }
}
```

**Key Methods:**
- `Bind(object subscriber)`: Registers all methods with `[OnGameEvent]` attributes
- `Unbind(object subscriber)`: Unregisters all methods registered with `Bind()`
- `Listen<TEvent>(...)`: Dynamically subscribes to an event
- `Publish(GameEvent evt)`: Publishes an event to all subscribers

### OnGameEventAttribute

Attribute for marking methods that should handle specific events.

```csharp
namespace Ludo.Core.EventBus
{
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
```

**Usage:**
```csharp
[OnGameEvent(typeof(PlayerDamagedEvent), SubscriberPriority.High)]
private void HandlePlayerDamage(PlayerDamagedEvent evt)
{
    // Handle the event
}
```

### SubscriberPriority

Enum defining the priority levels for event subscribers.

```csharp
namespace Ludo.Core.EventBus
{
    public enum SubscriberPriority
    {
        Essential = 0, // Always executed first, even if canceled
        High = 1,      // Default for static subscribers
        Medium = 2,    // Default for dynamic subscribers
        Low = 3,       // Final standard level
        Cleanup = 4    // Always executed last, even if canceled
    }
}
```

**Priority Order:**
1. `Essential` (0): Always executed first, even if the event is canceled
2. `High` (1): Default for attribute-based subscribers
3. `Medium` (2): Default for dynamic subscribers
4. `Low` (3): Lower priority handlers
5. `Cleanup` (4): Always executed last, even if the event is canceled

## Internal Classes

These classes are used internally by the EventBus system but may be useful to understand:

### EventSubscription

Represents a single subscription to an event.

```csharp
namespace Ludo.Core.EventBus
{
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
```

## Diagnostics Classes

The EventBus includes a diagnostics system for monitoring and debugging events.

### EventDiagnostics

The main class for event diagnostics and performance monitoring.

```csharp
namespace Ludo.Core.EventBus.Diagnostics
{
    public class EventDiagnostics
    {
        // Analysis methods
        public static IEnumerable<EventAnalysis> GetSlowSubscribers(int thresholdMs = 16);
        public static IEnumerable<EventTypeAnalysis> GetEventFrequency();
        public static IEnumerable<EventTrace> GetRecentTraces();
        public static IEnumerable<EventTrace> GetRecentTraces(Type eventType = null);
        public static void ClearDiagnostics();
    }
}
```

**Key Methods:**
- `GetSlowSubscribers()`: Returns subscribers that take longer than the threshold to process
- `GetEventFrequency()`: Returns statistics about event frequency
- `GetRecentTraces()`: Returns recent event traces for debugging
- `ClearDiagnostics()`: Clears all diagnostic data

### EventTrace

Represents a single event's execution trace.

```csharp
namespace Ludo.Core.EventBus.Diagnostics
{
    public class EventTrace
    {
        public string EventId { get; set; }
        public Type EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public List<SubscriberInvocation> Invocations { get; }
        public long TotalDurationMs { get; set; }
        public bool WasCanceled { get; set; }
        public string PublisherStackTrace { get; set; }
    }
}
```

### SubscriberInvocation

Represents a single subscriber's invocation within an event trace.

```csharp
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
```

### EventAnalysis

Contains performance analysis data for a subscriber.

```csharp
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
```

### EventTypeAnalysis

Contains frequency analysis data for an event type.

```csharp
namespace Ludo.Core.EventBus.Diagnostics
{
    public class EventTypeAnalysis
    {
        public Type EventType { get; set; }
        public int Frequency { get; set; }
    }
}
```

## Editor Classes

### EventDiagnosticsWindow

A Unity Editor window for visualizing event diagnostics.

```csharp
namespace Ludo.Core.EventBus
{
    public class EventDiagnosticsWindow : EditorWindow
    {
        [MenuItem("Window/Ludo/Analysis/Event Diagnostics")]
        public static void ShowWindow();
    }
}
```

**Features:**
- Overview of recent events
- Performance analysis of subscribers
- Event flow visualization
- Real-time monitoring
- Error detection
