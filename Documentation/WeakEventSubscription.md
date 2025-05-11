# WeakEventSubscription

The `WeakEventSubscription` class is a key component of the EventBus system that helps prevent memory leaks by holding weak references to subscribers.

## Overview

When objects subscribe to events, they typically create a strong reference from the event system to the subscriber. If the subscriber is destroyed without properly unsubscribing, this can lead to memory leaks. The `WeakEventSubscription` class solves this problem by using weak references, which allow the garbage collector to collect the subscriber even if it's still referenced by the event system.

## Class Definition

```csharp
public class WeakEventSubscription
{
    public Type EventType { get; private set; }
    private WeakReference _targetInstanceRef;
    public MethodInfo Method { get; private set; }
    public SubscriberPriority Priority { get; private set; }
    public bool IsDynamic { get; private set; }
    private Action<GameEvent> _dynamicCallback;
    
    // Cache the type of the target instance for faster checks
    private readonly Type _targetType;
    private readonly bool _isUnityObject;
    
    public object TargetInstance => _targetInstanceRef.IsAlive ? _targetInstanceRef.Target : null;
    
    public Action<GameEvent> DynamicCallback => 
        IsDynamic && _targetInstanceRef.IsAlive ? _dynamicCallback : null;
    
    // Constructor and methods...
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `EventType` | `Type` | The type of event this subscription is for |
| `Method` | `MethodInfo` | The method to invoke for static subscriptions |
| `Priority` | `SubscriberPriority` | The priority level of this subscription |
| `IsDynamic` | `bool` | Whether this is a dynamic (callback-based) subscription |
| `TargetInstance` | `object` | Gets the target instance if it's still alive, otherwise returns null |
| `DynamicCallback` | `Action<GameEvent>` | Gets the dynamic callback if this is a dynamic subscription and the target is still alive |

## Constructor

```csharp
public WeakEventSubscription(
    Type eventType, 
    object targetInstance, 
    MethodInfo method, 
    SubscriberPriority priority, 
    bool isDynamic, 
    Action<GameEvent> dynamicCallback)
```

Creates a new weak event subscription with the specified parameters:

- `eventType`: The type of event this subscription is for
- `targetInstance`: The object that will receive the event
- `method`: The method to invoke for static subscriptions (can be null for dynamic subscriptions)
- `priority`: The priority level of this subscription
- `isDynamic`: Whether this is a dynamic (callback-based) subscription
- `dynamicCallback`: The callback to invoke for dynamic subscriptions (can be null for static subscriptions)

## Methods

### IsValid

```csharp
public bool IsValid()
```

Checks if the subscription is still valid (target is alive and not destroyed).

- For regular objects, this checks if the weak reference is still alive
- For Unity objects, this also checks if the object has been destroyed using Unity's null check

### Invoke

```csharp
public void Invoke(GameEvent evt)
```

Invokes the subscription with the given event:

- For static subscriptions, it uses reflection to invoke the method on the target instance
- For dynamic subscriptions, it calls the dynamic callback directly

## Usage in the EventBus System

The `WeakEventSubscription` class is used internally by the `GameEventHub` to store subscriptions:

```csharp
// In GameEventHub.cs
private static ConcurrentDictionary<Type, List<WeakEventSubscription>> _subscriptions
    = new ConcurrentDictionary<Type, List<WeakEventSubscription>>();
```

When a subscriber registers for an event, a `WeakEventSubscription` is created and added to the appropriate list:

```csharp
// Example from GameEventHub.Bind method
var subscription = new WeakEventSubscription(
    attr.EventType,
    subscriber,
    method,
    attr.Priority,
    false,
    null
);
AddSubscription(subscription);
```

During event publishing, invalid subscriptions (where the target has been garbage collected or destroyed) are automatically removed:

```csharp
// Example from GameEventHub.Publish method
foreach (var subscription in sorted)
{
    // Skip invalid subscriptions
    if (!subscription.IsValid())
    {
        invalidSubscriptions.Add(subscription);
        continue;
    }
    
    // Invoke the subscription...
}

// Remove invalid subscriptions
if (invalidSubscriptions.Count > 0)
{
    foreach (var invalid in invalidSubscriptions)
    {
        subscribers.Remove(invalid);
    }
}
```

## Benefits

1. **Memory Safety**: Prevents memory leaks by allowing subscribers to be garbage collected
2. **Automatic Cleanup**: Invalid subscriptions are automatically removed during event publishing
3. **Unity Integration**: Special handling for Unity objects to detect when they've been destroyed
4. **Performance**: Caches type information for faster validity checks

## Comparison with EventSubscription

The `WeakEventSubscription` class is an improvement over the older `EventSubscription` class:

| Feature | WeakEventSubscription | EventSubscription |
|---------|----------------------|-------------------|
| Reference Type | Weak reference | Strong reference |
| Memory Safety | Prevents memory leaks | Can cause memory leaks |
| Unity Integration | Detects destroyed objects | No special handling |
| Performance | Caches type information | No caching |

## Best Practices

1. **Always Unsubscribe**: Even though `WeakEventSubscription` helps prevent memory leaks, it's still good practice to explicitly unsubscribe when an object is destroyed
2. **Use EventSubscriber Base Class**: For MonoBehaviours, use the `EventSubscriber` base class to automatically handle subscription lifecycle
3. **Store Unsubscribe Actions**: For dynamic subscriptions, store the returned unsubscribe action and call it when the object is destroyed
