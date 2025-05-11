# Ludo Core EventBus - Architecture

This document explains the internal architecture and design of the EventBus system.

## Table of Contents

- [System Overview](#system-overview)
- [Core Components](#core-components)
- [Subscription Lifecycle](#subscription-lifecycle)
- [Event Processing Flow](#event-processing-flow)
- [Memory Management](#memory-management)
- [Diagnostics System](#diagnostics-system)

## System Overview

The EventBus is designed as a publish-subscribe (pub/sub) messaging system with the following key design goals:

1. **Decoupling**: Components communicate without direct references to each other
2. **Type Safety**: Compile-time type checking for event handlers
3. **Performance**: Efficient event dispatching with minimal overhead
4. **Memory Safety**: Preventing memory leaks from lingering subscriptions
5. **Diagnostics**: Tools for monitoring and debugging event flow

## Core Components

### GameEvent

The base class for all events in the system:

```csharp
public abstract class GameEvent
{
    public bool IsCanceled { get; private set; }

    public void StopPropagation()
    {
        IsCanceled = true;
    }
}
```

### GameEventHub

The central hub that manages subscriptions and publishes events:

```csharp
public static class GameEventHub
{
    // Key: Event type, Value: list of subscribers
    private static ConcurrentDictionary<Type, List<WeakEventSubscription>> _subscriptions;
    
    // Public API methods
    public static void Bind(object subscriber) { ... }
    public static void Unbind(object subscriber) { ... }
    public static Action Listen<TEvent>(object subscriber, Action<TEvent> callback, SubscriberPriority priority) { ... }
    public static void Publish(GameEvent evt) { ... }
    public static void ConfigureGarbageCollection(float intervalSeconds) { ... }
    public static void CollectGarbage() { ... }
}
```

### WeakEventSubscription

Holds subscription data with a weak reference to the subscriber:

```csharp
public class WeakEventSubscription
{
    public Type EventType { get; private set; }
    private WeakReference _targetInstanceRef;
    public MethodInfo Method { get; private set; }
    public SubscriberPriority Priority { get; private set; }
    public bool IsDynamic { get; private set; }
    private Action<GameEvent> _dynamicCallback;
    
    // Methods
    public bool IsValid() { ... }
    public void Invoke(GameEvent evt) { ... }
    public object TargetInstance => _targetInstanceRef.Target;
}
```

### OnGameEventAttribute

Attribute for marking static subscriber methods:

```csharp
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
```

### EventSubscriber

Base class for MonoBehaviours that subscribe to events:

```csharp
public abstract class EventSubscriber : MonoBehaviour
{
    private readonly List<Action> _unsubscribers = new List<Action>();
    
    protected virtual void Awake()
    {
        GameEventHub.Bind(this);
    }
    
    protected virtual void OnDestroy()
    {
        GameEventHub.Unbind(this);
        
        foreach (var unsubscribe in _unsubscribers)
        {
            unsubscribe?.Invoke();
        }
        
        _unsubscribers.Clear();
    }
    
    protected Action<TEvent> Subscribe<TEvent>(Action<TEvent> callback, 
        SubscriberPriority priority = SubscriberPriority.Medium) 
        where TEvent : GameEvent
    {
        var unsubscribe = GameEventHub.Listen(this, callback, priority);
        _unsubscribers.Add(unsubscribe);
        
        return callback;
    }
}
```

### EventBusCleanupComponent

Component that automatically cleans up event subscriptions when a GameObject is destroyed:

```csharp
[AddComponentMenu("")] // Hide from Add Component menu
public class EventBusCleanupComponent : MonoBehaviour
{
    private MonoBehaviour _target;
    
    public void Initialize(MonoBehaviour target)
    {
        _target = target;
    }
    
    private void OnDestroy()
    {
        if (_target != null)
        {
            GameEventHub.Unbind(_target);
        }
    }
}
```

## Subscription Lifecycle

### Static Subscriptions

1. A MonoBehaviour calls `GameEventHub.Bind(this)` in its `Awake` method
2. The EventBus scans the object for methods with the `[OnGameEvent]` attribute
3. For each matching method, a `WeakEventSubscription` is created and added to the appropriate event type list
4. An `EventBusCleanupComponent` is added to the GameObject to handle automatic cleanup
5. When the GameObject is destroyed, the `EventBusCleanupComponent` calls `GameEventHub.Unbind(target)`
6. The EventBus removes all static subscriptions for the target object

### Dynamic Subscriptions

1. A component calls `GameEventHub.Listen<TEvent>(this, callback, priority)`
2. The EventBus creates a `WeakEventSubscription` and adds it to the appropriate event type list
3. The method returns an unsubscribe action that the caller should store
4. When the component is done with the subscription, it calls the unsubscribe action
5. The EventBus removes the dynamic subscription for the target object

## Event Processing Flow

When an event is published:

1. The publisher creates an event instance and calls `GameEventHub.Publish(event)`
2. The EventBus checks if garbage collection is needed and performs it if necessary
3. The EventBus retrieves the list of subscribers for the event type
4. Subscribers are sorted by priority (Essential → High → Medium → Low → Cleanup)
5. For each valid subscriber:
   - If the event is canceled and the subscriber's priority is not Essential or Cleanup, it is skipped
   - Otherwise, the subscriber's handler method is invoked with the event
6. Invalid subscriptions (where the target object has been garbage collected) are removed
7. Diagnostic information is recorded (in Editor and Development builds)

## Memory Management

The EventBus uses several strategies to prevent memory leaks:

1. **Weak References**: Subscriptions hold weak references to subscriber objects, allowing them to be garbage collected
2. **Automatic Cleanup**: For MonoBehaviours, an `EventBusCleanupComponent` is added to automatically unsubscribe when the GameObject is destroyed
3. **Unsubscribe Actions**: Dynamic subscriptions return an unsubscribe action that should be called when the subscription is no longer needed
4. **Periodic Garbage Collection**: The EventBus periodically checks for and removes invalid subscriptions

The garbage collection interval can be configured:

```csharp
// Set garbage collection interval to 60 seconds
GameEventHub.ConfigureGarbageCollection(60f);

// Disable automatic garbage collection
GameEventHub.ConfigureGarbageCollection(0f);
```

## Diagnostics System

In Editor and Development builds, the EventBus includes a diagnostics system that tracks:

1. **Event Traces**: Records of all published events, including their subscribers and execution times
2. **Slow Subscribers**: Subscribers that take longer than a threshold to process events
3. **Event Frequency**: How often each event type is published

This information can be viewed in the EventDiagnostics window (Window > Ludo > Analysis > Event Diagnostics).
