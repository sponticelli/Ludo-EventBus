# Ludo EventBus - Patterns & Anti-patterns

This document outlines recommended patterns and anti-patterns when using the Ludo EventBus system.

## Recommended Patterns

### 1. Event Naming Conventions

Use clear, descriptive names for your events:

✅ **Good:**
```csharp
PlayerDamagedEvent
EnemySpawnedEvent
LevelCompletedEvent
```

❌ **Bad:**
```csharp
DamageEvent
SpawnEvent
LevelEvent
```

### 2. Event Organization

Group related events in a logical namespace structure:

✅ **Good:**
```csharp
namespace MyGame.Combat.Events
{
    public class PlayerDamagedEvent : GameEvent { ... }
    public class EnemyDamagedEvent : GameEvent { ... }
}

namespace MyGame.Progression.Events
{
    public class LevelCompletedEvent : GameEvent { ... }
    public class AchievementUnlockedEvent : GameEvent { ... }
}
```

### 3. Immutable Events

Make events immutable to prevent unexpected changes:

✅ **Good:**
```csharp
public class PlayerDamagedEvent : GameEvent
{
    public int DamageAmount { get; }
    
    public PlayerDamagedEvent(int damageAmount)
    {
        DamageAmount = damageAmount;
    }
}
```

❌ **Bad:**
```csharp
public class PlayerDamagedEvent : GameEvent
{
    public int DamageAmount { get; set; }
}
```

### 4. Proper Lifecycle Management

Always bind/unbind subscribers at the appropriate lifecycle points:

✅ **Good:**
```csharp
private void Awake()
{
    GameEventHub.Bind(this);
}

private void OnDestroy()
{
    GameEventHub.Unbind(this);
}
```

For dynamic subscriptions:

✅ **Good:**
```csharp
private Action _unsubscribe;

private void OnEnable()
{
    _unsubscribe = GameEventHub.Listen<MyEvent>(this, HandleEvent);
}

private void OnDisable()
{
    _unsubscribe?.Invoke();
    _unsubscribe = null;
}
```

### 5. Use Event Inheritance Wisely

Create base events for common functionality:

✅ **Good:**
```csharp
// Base event
public abstract class EntityDamagedEvent : GameEvent
{
    public int DamageAmount { get; }
    public string DamageSource { get; }
    
    protected EntityDamagedEvent(int damageAmount, string damageSource)
    {
        DamageAmount = damageAmount;
        DamageSource = damageSource;
    }
}

// Specific events
public class PlayerDamagedEvent : EntityDamagedEvent
{
    public PlayerDamagedEvent(int damageAmount, string damageSource) 
        : base(damageAmount, damageSource) { }
}

public class EnemyDamagedEvent : EntityDamagedEvent
{
    public EnemyType EnemyType { get; }
    
    public EnemyDamagedEvent(int damageAmount, string damageSource, EnemyType enemyType) 
        : base(damageAmount, damageSource)
    {
        EnemyType = enemyType;
    }
}
```

### 6. Use Appropriate Priorities

Assign priorities based on the logical order of operations:

✅ **Good:**
```csharp
// Essential: Check if damage should be applied at all
[OnGameEvent(typeof(PlayerDamagedEvent), SubscriberPriority.Essential)]
private void CheckInvulnerability(PlayerDamagedEvent evt)
{
    if (_invulnerable) evt.StopPropagation();
}

// High: Core game logic
[OnGameEvent(typeof(PlayerDamagedEvent), SubscriberPriority.High)]
private void ApplyDamage(PlayerDamagedEvent evt)
{
    _health -= evt.DamageAmount;
}

// Medium: Secondary effects
[OnGameEvent(typeof(PlayerDamagedEvent), SubscriberPriority.Medium)]
private void PlayDamageEffects(PlayerDamagedEvent evt)
{
    _damageVfx.Play();
}

// Cleanup: Analytics, logging
[OnGameEvent(typeof(PlayerDamagedEvent), SubscriberPriority.Cleanup)]
private void LogDamage(PlayerDamagedEvent evt)
{
    Analytics.LogEvent("player_damaged", evt.DamageAmount);
}
```

## Anti-patterns

### 1. Event Overload

❌ **Anti-pattern:** Creating too many fine-grained events.

**Problem:** This leads to complexity and makes it hard to understand the system.

**Solution:** Find the right balance. Group related events and use properties to distinguish variations.

### 2. Mutable Events

❌ **Anti-pattern:** Modifying event data after creation.

**Problem:** This can lead to unpredictable behavior as subscribers might see different data.

**Solution:** Make events immutable with read-only properties.

### 3. Circular Event Dependencies

❌ **Anti-pattern:** Event handlers that publish events that trigger the original handler.

```csharp
[OnGameEvent(typeof(PlayerDamagedEvent))]
private void OnPlayerDamaged(PlayerDamagedEvent evt)
{
    // This could create an infinite loop!
    GameEventHub.Publish(new PlayerHealthChangedEvent(_health));
}

[OnGameEvent(typeof(PlayerHealthChangedEvent))]
private void OnHealthChanged(PlayerHealthChangedEvent evt)
{
    // This might trigger PlayerDamagedEvent again
    if (evt.Health < _previousHealth)
    {
        GameEventHub.Publish(new PlayerDamagedEvent(_previousHealth - evt.Health));
    }
    _previousHealth = evt.Health;
}
```

**Problem:** This can create infinite loops or hard-to-debug cascades.

**Solution:** Be careful with event chains and avoid circular dependencies.

### 4. Memory Leaks from Missing Unbind

❌ **Anti-pattern:** Forgetting to unbind subscribers.

**Problem:** This causes memory leaks and can lead to errors when destroyed objects receive events.

**Solution:** Always pair `Bind` with `Unbind` and store/invoke unsubscribe actions for dynamic subscriptions.

### 5. Using Events for Direct Communication

❌ **Anti-pattern:** Using events as a replacement for direct method calls between tightly coupled components.

**Problem:** This adds unnecessary complexity and makes the code harder to follow.

**Solution:** Use events for decoupling systems, not for communication within a single system.

### 6. Ignoring Event Performance

❌ **Anti-pattern:** Creating high-frequency events without considering performance.

**Problem:** This can cause performance issues, especially on mobile devices.

**Solution:** Use the diagnostics tools to monitor event performance and optimize high-frequency events.

### 7. Overly Generic Events

❌ **Anti-pattern:** Creating vague events with generic names and purposes.

```csharp
// Too generic
public class GameEvent : GameEvent
{
    public string EventType { get; }
    public Dictionary<string, object> Data { get; }
}
```

**Problem:** This loses type safety and makes the code harder to understand and maintain.

**Solution:** Create specific, well-named event classes for each distinct event type.

### 8. Excessive Event Cancellation

❌ **Anti-pattern:** Overusing event cancellation.

**Problem:** This makes the system unpredictable and hard to debug.

**Solution:** Use cancellation sparingly and for clear use cases like permission checks.

## Best Practices Summary

1. **Design events carefully** - Create a clear event hierarchy with descriptive names
2. **Make events immutable** - Set all properties in the constructor
3. **Manage lifecycles properly** - Always unbind subscribers when they're destroyed
4. **Use appropriate priorities** - Assign priorities based on logical execution order
5. **Monitor performance** - Use the diagnostics tools to identify slow subscribers
6. **Document your events** - Maintain a catalog of events and their purposes
7. **Test event flows** - Verify that events propagate correctly and handlers work as expected
