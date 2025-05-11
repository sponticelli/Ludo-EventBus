# Ludo Core EventBus - Usage Guide

This guide provides detailed examples and best practices for using the EventBus system in your Unity projects.

## Table of Contents

- [Creating Custom Events](#creating-custom-events)
- [Subscribing to Events](#subscribing-to-events)
  - [Static Subscriptions](#static-subscriptions)
  - [Dynamic Subscriptions](#dynamic-subscriptions)
  - [Using EventSubscriber Base Class](#using-eventsubscriber-base-class)
- [Publishing Events](#publishing-events)
- [Event Priorities](#event-priorities)
- [Event Cancellation](#event-cancellation)
- [Memory Management](#memory-management)
- [IL2CPP/AOT Considerations](#il2cpp-aot-considerations)

## Creating Custom Events

All events must inherit from the `GameEvent` base class:

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

Best practices for custom events:

1. Make properties read-only when possible to prevent subscribers from modifying event data
2. Add the `[Preserve]` attribute to ensure the event class isn't stripped in IL2CPP builds
3. Keep events immutable - set all data in the constructor
4. Use descriptive names that clearly indicate what happened (past tense)

## Subscribing to Events

The EventBus system offers three ways to subscribe to events:

### Static Subscriptions

Static subscriptions use the `[OnGameEvent]` attribute to mark methods that should handle specific events:

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
        // Unregister all methods with [OnGameEvent] attribute
        GameEventHub.Unbind(this);
    }
    
    [OnGameEvent(typeof(PlayerDamagedEvent), SubscriberPriority.High)]
    private void OnPlayerDamaged(PlayerDamagedEvent evt)
    {
        Debug.Log($"Player took {evt.DamageAmount} damage from {evt.DamageSource}");
    }
    
    // You can subscribe to multiple event types with different methods
    [OnGameEvent(typeof(PlayerHealedEvent), SubscriberPriority.Medium)]
    private void OnPlayerHealed(PlayerHealedEvent evt)
    {
        Debug.Log($"Player healed for {evt.HealAmount}");
    }
    
    // You can also subscribe to the same event multiple times
    [OnGameEvent(typeof(PlayerDamagedEvent), SubscriberPriority.Cleanup)]
    private void LogDamage(PlayerDamagedEvent evt)
    {
        Debug.Log($"LOGGING: Player took damage: {evt.DamageAmount}");
    }
}
```

### Dynamic Subscriptions

Dynamic subscriptions allow you to subscribe to events at runtime with lambda expressions or method references:

```csharp
using Ludo.Core.EventBus;
using UnityEngine;
using System;

public class UIManager : MonoBehaviour
{
    private Action _unsubscriber;
    private Action _anotherUnsubscriber;
    
    private void Start()
    {
        // Subscribe with a method reference
        _unsubscriber = GameEventHub.Listen<PlayerDamagedEvent>(
            this,
            OnPlayerDamaged,
            SubscriberPriority.Medium
        );
        
        // Subscribe with a lambda expression
        _anotherUnsubscriber = GameEventHub.Listen<PlayerHealedEvent>(
            this,
            evt => UpdateHealthUI(evt.HealAmount),
            SubscriberPriority.Low
        );
    }
    
    private void OnDestroy()
    {
        // Always unsubscribe when the GameObject is destroyed
        _unsubscriber?.Invoke();
        _anotherUnsubscriber?.Invoke();
    }
    
    private void OnPlayerDamaged(PlayerDamagedEvent evt)
    {
        UpdateHealthUI(-evt.DamageAmount);
    }
    
    private void UpdateHealthUI(int healthChange)
    {
        // Update UI elements
    }
}
```

### Using EventSubscriber Base Class

The `EventSubscriber` base class simplifies event subscription management:

```csharp
using Ludo.Core.EventBus;
using UnityEngine;

public class DamageEffects : EventSubscriber
{
    protected override void Awake()
    {
        base.Awake(); // This calls GameEventHub.Bind(this)
        
        // Dynamic subscriptions are automatically cleaned up
        Subscribe<PlayerDamagedEvent>(OnPlayerDamaged, SubscriberPriority.Low);
        Subscribe<PlayerHealedEvent>(OnPlayerHealed, SubscriberPriority.Low);
    }
    
    private void OnPlayerDamaged(PlayerDamagedEvent evt)
    {
        // Play damage effects
    }
    
    private void OnPlayerHealed(PlayerHealedEvent evt)
    {
        // Play healing effects
    }
    
    // No need to manually unbind - base.OnDestroy() handles it
}
```

## Publishing Events

Publishing events is straightforward:

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
        
        // You can check if the event was canceled by any subscriber
        if (damageEvent.IsCanceled)
        {
            Debug.Log("Damage was canceled by a subscriber!");
        }
    }
}
```

## Event Priorities

The EventBus system uses priorities to determine the order in which subscribers receive events:

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

Guidelines for using priorities:

- **Essential**: Use for critical systems that must always run (e.g., logging, analytics)
- **High**: Use for core gameplay systems that should run before visual effects
- **Medium**: Use for most gameplay systems
- **Low**: Use for visual effects, audio, and other non-critical systems
- **Cleanup**: Use for final cleanup operations that should always run

## Event Cancellation

Events can be canceled to prevent further processing by subscribers with lower priorities:

```csharp
[OnGameEvent(typeof(PlayerDamagedEvent), SubscriberPriority.High)]
private void OnPlayerDamaged(PlayerDamagedEvent evt)
{
    // Check if player has invincibility
    if (IsPlayerInvincible)
    {
        // Cancel the event to prevent damage
        evt.StopPropagation();
        
        // Show invincibility effect
        ShowInvincibilityEffect();
    }
}
```

Important notes about cancellation:

- Subscribers with `Essential` and `Cleanup` priorities will still receive canceled events
- Cancellation only affects subscribers with lower priorities than the one that canceled the event
- You can check if an event was canceled using the `IsCanceled` property

## Memory Management

The EventBus system uses weak references to prevent memory leaks:

- For MonoBehaviours, subscriptions are automatically cleaned up when the GameObject is destroyed
- For non-MonoBehaviour objects, you should manually unsubscribe when the object is no longer needed
- The system performs periodic garbage collection to remove stale subscriptions

You can configure the garbage collection interval:

```csharp
// Set garbage collection interval to 60 seconds
GameEventHub.ConfigureGarbageCollection(60f);

// Disable automatic garbage collection
GameEventHub.ConfigureGarbageCollection(0f);

// Manually trigger garbage collection
GameEventHub.CollectGarbage();
```

## IL2CPP/AOT Considerations

When using IL2CPP or other AOT compilation methods, you need to ensure that generic types are preserved:

1. Add the `[Preserve]` attribute to all event classes
2. Register all event types in a preservation class:

```csharp
using System;
using System.Runtime.CompilerServices;
using Ludo.Core.EventBus;
using UnityEngine.Scripting;

public static class AOTEventBusPreservation
{
    [Preserve]
    public static void PreserveAll()
    {
        // Register all your event types here
        PreserveEvent<PlayerDamagedEvent>();
        PreserveEvent<PlayerHealedEvent>();
        PreserveEvent<GameStartEvent>();
        PreserveEvent<GameOverEvent>();
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PreserveEvent<T>() where T : GameEvent
    {
        // This forces the compiler to generate the generic code
        GameEventHub.Listen<T>(null, _ => { }, SubscriberPriority.Medium);
    }
}
```