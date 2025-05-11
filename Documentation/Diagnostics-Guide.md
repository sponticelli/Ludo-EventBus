# Ludo Core EventBus - Diagnostics

The EventBus system includes a comprehensive diagnostics system to help you monitor, debug, and optimize your event-based architecture. This document explains how to use these diagnostic tools.

## Table of Contents

- [Overview](#overview)
- [Enabling Diagnostics](#enabling-diagnostics)
- [Using the Diagnostics Window](#using-the-diagnostics-window)
   - [Overview Tab](#overview-tab)
   - [Event Flow Tab](#event-flow-tab)
   - [Performance Tab](#performance-tab)
   - [Monitoring Tab](#monitoring-tab)
- [Programmatic Access](#programmatic-access)
- [Best Practices](#best-practices)

## Overview

The diagnostics system tracks:

1. **Event Traces**: Complete records of all published events, including:
   - Event type and timestamp
   - Publisher stack trace
   - List of subscribers that processed the event
   - Execution time for each subscriber
   - Whether the event was canceled

2. **Performance Metrics**:
   - Average and maximum execution time for each subscriber
   - Event frequency (how often each event type is published)
   - Slow subscribers that exceed a performance threshold

3. **Runtime Monitoring**:
   - Real-time view of event flow
   - Filtering by event type
   - Detailed inspection of individual events

## Enabling Diagnostics

The diagnostics system is automatically enabled in the Unity Editor and in Development builds. It is disabled in Release builds to avoid any performance overhead.

To enable or disable diagnostics in specific builds, you can use the following scripting define symbols:

- `ENABLE_EVENTBUS_DIAGNOSTICS`: Force enable diagnostics in all builds
- `DISABLE_EVENTBUS_DIAGNOSTICS`: Force disable diagnostics in all builds

Add these to your project's Scripting Define Symbols in the Player Settings.

## Using the Diagnostics Window

To open the diagnostics window, go to **Window > Ludo > Analysis > Event Diagnostics**.

### Overview Tab

The Overview tab provides a high-level summary of your event system:

- **Event Types**: List of all event types that have been published
- **Total Events**: Total number of events published
- **Active Subscriptions**: Number of active subscriptions
- **Slow Subscribers**: Number of subscribers that exceed the performance threshold
- **Memory Usage**: Estimated memory usage of the event system

### Event Flow Tab

The Event Flow tab shows a chronological list of all published events:

- **Event Type**: The type of the event
- **Timestamp**: When the event was published
- **Duration**: Total time to process the event
- **Subscribers**: Number of subscribers that processed the event
- **Canceled**: Whether the event was canceled

You can click on an event to see detailed information:

- **Publisher Stack Trace**: Where the event was published from
- **Subscriber List**: All subscribers that processed the event, with their priorities and execution times
- **Cancellation Point**: If the event was canceled, which subscriber canceled it

### Performance Tab

The Performance tab helps you identify performance issues:

- **Slow Subscribers**: Subscribers that take longer than the threshold to process events
- **Event Frequency**: How often each event type is published
- **Execution Time Distribution**: Histogram of execution times for all subscribers

You can adjust the threshold for slow subscribers using the slider at the top of the tab.

### Monitoring Tab

The Monitoring tab provides real-time monitoring of event flow:

- **Live Event Stream**: Real-time view of events as they are published
- **Event Type Filter**: Filter events by type
- **Auto-Scroll**: Automatically scroll to show the latest events
- **Clear**: Clear the current view

## Programmatic Access

You can access diagnostic data programmatically using the `EventDiagnostics` class:

```csharp
using Ludo.Core.EventBus.Diagnostics;
using UnityEngine;

public class DiagnosticsExample : MonoBehaviour
{
    public void LogDiagnosticInfo()
    {
        // Get slow subscribers (those taking more than 16ms to process events)
        var slowSubscribers = EventDiagnostics.GetSlowSubscribers(16);
        foreach (var subscriber in slowSubscribers)
        {
            Debug.Log($"Slow subscriber: {subscriber.Name}, " +
                      $"Avg: {subscriber.AverageDurationMs}ms, " +
                      $"Max: {subscriber.MaxDurationMs}ms, " +
                      $"Count: {subscriber.InvocationCount}");
        }
        
        // Get event frequency
        var eventFrequency = EventDiagnostics.GetEventFrequency();
        foreach (var evt in eventFrequency)
        {
            Debug.Log($"Event: {evt.EventType.Name}, Frequency: {evt.Frequency}");
        }
        
        // Get recent traces
        var recentTraces = EventDiagnostics.GetRecentTraces();
        foreach (var trace in recentTraces)
        {
            Debug.Log($"Event: {trace.EventType.Name}, " +
                      $"Time: {trace.Timestamp}, " +
                      $"Duration: {trace.TotalDurationMs}ms, " +
                      $"Canceled: {trace.WasCanceled}");
        }
        
        // Get traces for a specific event type
        var playerDamageTraces = EventDiagnostics.GetRecentTraces(typeof(PlayerDamagedEvent));
        
        // Clear all diagnostic data
        EventDiagnostics.ClearDiagnostics();
    }
}
```

## Best Practices

1. **Monitor Event Frequency**: High-frequency events can cause performance issues. Consider batching or throttling events that occur very frequently.

2. **Watch for Slow Subscribers**: Subscribers that take more than 16ms (one frame at 60 FPS) to process events can cause frame drops. Optimize or move expensive operations to coroutines.

3. **Check for Canceled Events**: If events are frequently canceled, it might indicate a design issue. Consider refactoring to avoid unnecessary event publishing.

4. **Use Filtering**: When investigating issues, use the event type filter to focus on specific events.

5. **Clear Diagnostics**: The diagnostics system stores event traces in memory. Clear diagnostics periodically to avoid excessive memory usage during long play sessions.

6. **Disable in Release**: Make sure diagnostics are disabled in release builds to avoid performance overhead.