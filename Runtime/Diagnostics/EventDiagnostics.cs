using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ludo.Core.EventBus.Diagnostics
{
    public class EventDiagnostics
    {
        // Store recent event traces
        private static readonly ConcurrentQueue<EventTrace> RecentTraces = new ConcurrentQueue<EventTrace>();
        private const int MaxStoredTraces = 1000;

        // Track slow subscribers
        private static readonly ConcurrentDictionary<string, List<long>> SubscriberPerformance =
            new ConcurrentDictionary<string, List<long>>();

        private static readonly ConcurrentDictionary<Type, int> EventTypeFrequency =
            new ConcurrentDictionary<Type, int>();

        public static EventTrace StartTrace(GameEvent evt)
        {
            var trace = new EventTrace
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = evt.GetType(),
                Timestamp = DateTime.UtcNow,
                PublisherStackTrace = Environment.StackTrace
            };

            // Store trace and maintain queue size
            RecentTraces.Enqueue(trace);
            while (RecentTraces.Count > MaxStoredTraces)
            {
                RecentTraces.TryDequeue(out _);
            }

            // Track event frequency
            EventTypeFrequency.AddOrUpdate(evt.GetType(), 1, (_, count) => count + 1);

            return trace;
        }

        public static void TrackSubscriberInvocation(
            EventTrace trace,
            EventSubscription subscription,
            Action invoke)
        {
            var stopwatch = Stopwatch.StartNew();
            Exception error = null;

            try
            {
                invoke();
            }
            catch (Exception ex)
            {
                error = ex;
            }

            stopwatch.Stop();

            var subscriberName = GetSubscriberName(subscription);
            var invocation = new SubscriberInvocation
            {
                SubscriberName = subscriberName,
                Priority = subscription.Priority,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Error = error,
                CanceledEvent = false
            };

            trace.Invocations.Add(invocation);

            // Track performance
            var perfKey = $"{subscriberName}_{trace.EventType.Name}";
            SubscriberPerformance.AddOrUpdate(
                perfKey,
                new List<long> { stopwatch.ElapsedMilliseconds },
                (_, list) =>
                {
                    list.Add(stopwatch.ElapsedMilliseconds);
                    if (list.Count > 100) list.RemoveAt(0);
                    return list;
                });
        }

        private static string GetSubscriberName(EventSubscription subscription)
        {
            if (subscription.IsDynamic)
                return $"{subscription.TargetInstance.GetType().Name}::DynamicHandler";
            return $"{subscription.TargetInstance.GetType().Name}::{subscription.Method.Name}";
        }

        public static void CompleteTrace(EventTrace trace, GameEvent evt)
        {
            trace.WasCanceled = evt.IsCanceled;
            trace.TotalDurationMs = trace.Invocations.Sum(i => i.DurationMs);
        }

        #region Analysis Methods

        public static IEnumerable<EventAnalysis> GetSlowSubscribers(int thresholdMs = 16)
        {
            return SubscriberPerformance
                .Where(kvp => kvp.Value.Average() > thresholdMs)
                .Select(kvp => new EventAnalysis
                {
                    Name = kvp.Key,
                    AverageDurationMs = kvp.Value.Average(),
                    MaxDurationMs = kvp.Value.Max(),
                    InvocationCount = kvp.Value.Count
                })
                .OrderByDescending(x => x.AverageDurationMs);
        }

        public static IEnumerable<EventTypeAnalysis> GetEventFrequency()
        {
            return EventTypeFrequency
                .Select(kvp => new EventTypeAnalysis
                {
                    EventType = kvp.Key,
                    Frequency = kvp.Value
                })
                .OrderByDescending(x => x.Frequency);
        }

        public static IEnumerable<EventTrace> GetRecentTraces()
        {
            return RecentTraces.ToList();
        }
        
        public static IEnumerable<EventTrace> GetRecentTraces(Type eventType = null)
        {
            var traces = RecentTraces.ToList();
            if (eventType != null)
                traces = traces.Where(t => t.EventType == eventType).ToList();
            return traces;
        }

        public static void ClearDiagnostics()
        {
            RecentTraces.Clear();
            SubscriberPerformance.Clear();
            EventTypeFrequency.Clear();
        }

        #endregion
    }
}