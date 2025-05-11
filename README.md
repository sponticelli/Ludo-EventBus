# Ludo EventBus

A lightweight, type-safe event system for Unity games with priority-based event handling and diagnostics.

## Features

- **Type-safe events**: All events are strongly typed
- **Priority-based handling**: Control the order of event handlers
- **Automatic cleanup**: Prevents memory leaks with weak references
- **Diagnostics**: Monitor event flow and performance
- **Cancellation**: Stop event propagation when needed

## Installation

### Via Unity Package Manager

1. Open the Package Manager window in Unity
2. Click the "+" button and select "Add package from git URL"
3. Enter: `https://github.com/sponticelli/Ludo-EventBus.git#main`

### Manual Installation

1. Clone this repository
2. Copy the `Assets/Ludo/Core/EventBus` folder to your Unity project

## Quick Start

### 1. Create an Event

Events are classes that inherit from `GameEvent`:

```csharp
using Ludo.Core.EventBus;
using UnityEngine.Scripting;

[Preserve] // Important for IL2CPP builds
public class PlayerDamagedEvent : GameEvent
{
    public int DamageAmount { get; }
    public string DamageSource { get; }
    
    public PlayerDamagedEvent(int damageAmount, string damageSource)
    {
        DamageAmount = damageAmount;
        DamageSource = damageSource;
    }
}
```

### 2. Subscribe to Events

#### Method 1: Using Attributes (Recommended)

```csharp
using Ludo.Core.EventBus;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    private void Awake()
    {
        // Register all methods with [OnGameEvent] attribute
        GameEventHub.Bind(this);
    }
    
    private void OnDestroy()
    {
        // Unregister all methods
        GameEventHub.Unbind(this);
    }
    
    [OnGameEvent(typeof(PlayerDamagedEvent), SubscriberPriority.High)]
    private void OnPlayerDamaged(PlayerDamagedEvent evt)
    {
        Debug.Log($"Player took {evt.DamageAmount} damage from {evt.DamageSource}");
        
        // You can stop other handlers from processing this event
        if (evt.DamageAmount > 100)
        {
            evt.StopPropagation();
        }
    }
}
```

#### Method 2: Using Dynamic Subscriptions

```csharp
using Ludo.Core.EventBus;
using UnityEngine;
using System;

public class DamageEffects : MonoBehaviour
{
    private Action _unsubscriber;
    
    private void Start()
    {
        // Subscribe to an event with a callback
        _unsubscriber = GameEventHub.Listen<PlayerDamagedEvent>(
            this, 
            OnPlayerDamaged, 
            SubscriberPriority.Medium
        );
    }
    
    private void OnDestroy()
    {
        // Unsubscribe when destroyed
        _unsubscriber?.Invoke();
    }
    
    private void OnPlayerDamaged(PlayerDamagedEvent evt)
    {
        // Play damage effects
    }
}
```

#### Method 3: Using EventSubscriber Base Class

```csharp
using Ludo.Core.EventBus;
using UnityEngine;

public class DamageUI : EventSubscriber
{
    protected override void Awake()
    {
        base.Awake(); // This calls GameEventHub_Improved.Bind(this)
        
        // You can also use dynamic subscriptions
        Subscribe<PlayerDamagedEvent>(OnPlayerDamaged, SubscriberPriority.Low);
    }
    
    [OnGameEvent(typeof(PlayerDamagedEvent))]
    private void HandleDamageUI(PlayerDamagedEvent evt)
    {
        // Update UI
    }
    
    private void OnPlayerDamaged(PlayerDamagedEvent evt)
    {
        // Alternative handler
    }
}
```

### 3. Publish Events

```csharp
using Ludo.Core.EventBus;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public void AttackPlayer(int damage)
    {
        // Create and publish the event
        var damageEvent = new PlayerDamagedEvent(damage, "Enemy");
        GameEventHub.Publish(damageEvent);
        
        // Check if the event was canceled
        if (damageEvent.IsCanceled)
        {
            Debug.Log("Damage was canceled by a handler!");
        }
    }
}
```

## Event Priorities

Event handlers are executed in order of priority:

```csharp
public enum SubscriberPriority
{
    Essential = 0, // Always executed first, even if canceled
    High = 1,      // Default for static subscribers
    Medium = 2,    // Default for dynamic subscribers
    Low = 3,       // Final standard level
    Cleanup = 4    // Always executed last, even if canceled
}
```

## Diagnostics

The EventBus includes a diagnostics system to help you monitor and debug events:

```csharp
using Ludo.Core.EventBus.Diagnostics;
using UnityEngine;

public class EventMonitor : MonoBehaviour
{
    public void LogSlowSubscribers()
    {
        var slowSubscribers = EventDiagnostics.GetSlowSubscribers();
        foreach (var sub in slowSubscribers)
        {
            Debug.Log($"{sub.Name}: {sub.AverageDurationMs}ms");
        }
    }
    
    public void LogEventFrequency()
    {
        var frequencies = EventDiagnostics.GetEventFrequency();
        foreach (var freq in frequencies)
        {
            Debug.Log($"{freq.EventType.Name}: {freq.Frequency} times");
        }
    }
}
```

In the Unity Editor, you can also use the Event Diagnostics window:
- Open via `Window > Ludo > Analysis > Event Diagnostics`

## IL2CPP Support

For IL2CPP builds, you need to preserve your event types:

```csharp
using Ludo.Core.EventBus;
using UnityEngine.Scripting;

// Add this attribute to all event classes
[Preserve]
public class MyEvent : GameEvent
{
    // ...
}
```

For AOT platforms, register your events in a preservation class:

```csharp
using UnityEngine.Scripting;
using Ludo.Core.EventBus;

public static class AOTEventBusPreservation
{
    [Preserve]
    public static void PreserveAll()
    {
        // Register all your event types here
        PreserveEvent<PlayerDamagedEvent>();
        PreserveEvent<GameStartEvent>();
        // ...
    }
    
    [Preserve]
    private static void PreserveEvent<T>() where T : GameEvent
    {
        // This ensures the generic methods are compiled for this type
        var evt = Activator.CreateInstance<T>();
        GameEventHub.Publish(evt);
    }
}
```

## Best Practices

1. Keep events small and focused
2. Use meaningful names for events
3. Consider event priority carefully
4. Always unsubscribe when objects are destroyed
5. Use the `[Preserve]` attribute for IL2CPP builds
6. Monitor performance with the diagnostics tools

## License

This project is licensed under the MIT License - see the LICENSE file for details.