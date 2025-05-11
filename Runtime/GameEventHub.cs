using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Ludo.Core.EventBus.Diagnostics;
#endif


namespace Ludo.Core.EventBus
{
    /// <summary>
    /// The main hub that allows publishing and subscribing to events.
    /// </summary>
    public static class GameEventHub
    {
        // Key: Event type, Value: list of subscribers
        private static ConcurrentDictionary<Type, List<WeakEventSubscription>> _subscriptions
            = new ConcurrentDictionary<Type, List<WeakEventSubscription>>();
            
        // Last time garbage collection was performed
        private static float _lastGcTime = 0f;
        
        // How often to perform garbage collection (in seconds)
        private static float _gcInterval = 30f;
        
        // Whether automatic cleanup is enabled
        private static bool _autoCleanupEnabled = true;

        #region Configuration
        
        /// <summary>
        /// Configure the garbage collection interval for stale subscriptions.
        /// </summary>
        /// <param name="intervalSeconds">Interval in seconds (0 to disable)</param>
        public static void ConfigureGarbageCollection(float intervalSeconds)
        {
            _gcInterval = intervalSeconds;
            _autoCleanupEnabled = intervalSeconds > 0;
        }
        
        #endregion

        #region Static Subscriptions

        /// <summary>
        /// Binds all methods in the 'subscriber' that have [OnGameEvent].
        /// </summary>
        public static void Bind(object subscriber)
        {
            var methods = subscriber.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(OnGameEventAttribute), true);
                foreach (OnGameEventAttribute attr in attributes)
                {
                    // Create a subscription
                    var subscription = new WeakEventSubscription(
                        attr.EventType,
                        subscriber,
                        method,
                        attr.Priority,
                        false,
                        null
                    );
                    AddSubscription(subscription);
                }
            }
            
            // Register for automatic cleanup if it's a Unity object
            RegisterForAutomaticCleanup(subscriber);
        }

        /// <summary>
        /// Unbinds all methods in the 'subscriber' that were registered via Bind().
        /// </summary>
        public static void Unbind(object subscriber)
        {
            foreach (var kvp in _subscriptions)
            {
                kvp.Value.RemoveAll(s => 
                    s.TargetInstance == subscriber && !s.IsDynamic);
            }
        }

        #endregion

        #region Dynamic Subscriptions

        /// <summary>
        /// Subscribes a single callback method to a specific event type.
        /// Returns an Action you should store and call to unsubscribe.
        /// </summary>
        public static Action Listen<TEvent>(object subscriber,
            Action<TEvent> callback,
            SubscriberPriority priority = SubscriberPriority.Medium)
            where TEvent : GameEvent
        {
            // Wrap the callback into a generic callback.
            Action<GameEvent> dynamicCallback = (e) => callback((TEvent)e);

            var subscription = new WeakEventSubscription(
                typeof(TEvent),
                subscriber,
                null,
                priority,
                true,
                dynamicCallback
            );
            AddSubscription(subscription);
            
            // Register for automatic cleanup if it's a Unity object
            RegisterForAutomaticCleanup(subscriber);

            // Return an unsubscribe action
            return () =>
            {
                if (_subscriptions.TryGetValue(typeof(TEvent), out var list))
                {
                    list.RemoveAll(s => 
                        s.TargetInstance == subscriber && 
                        s.IsDynamic);
                }
            };
        }

        #endregion

        #region Publish

        /// <summary>
        /// Publishes the event to all subscribers, in priority order.
        /// </summary>
        public static void Publish(GameEvent evt)
        {
            // Check if we need to perform garbage collection
            CheckForGarbageCollection();
            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var trace = EventDiagnostics.StartTrace(evt);
#endif
            try
            {
                var evtType = evt.GetType();
                if (!_subscriptions.TryGetValue(evtType, out var subscribers))
                    return;

                // Sort by priority (0=Essential -> 4=Cleanup)
                var sorted = subscribers.OrderBy(s => s.Priority).ToList();
                
                // Keep track of invalid subscriptions to remove after iteration
                List<WeakEventSubscription> invalidSubscriptions = new List<WeakEventSubscription>();

                foreach (var subscription in sorted)
                {
                    // Skip invalid subscriptions
                    if (!subscription.IsValid())
                    {
                        invalidSubscriptions.Add(subscription);
                        continue;
                    }
                    
                    // If it's canceled and subscription is not Essential or Cleanup, skip
                    if (evt.IsCanceled && subscription.Priority != SubscriberPriority.Essential
                                       && subscription.Priority != SubscriberPriority.Cleanup)
                    {
                        continue;
                    }

                    // Invoke
                    try
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        EventDiagnostics.TrackSubscriberInvocation(
                            trace,
                            ConvertToEventSubscription(subscription),
                            () => subscription.Invoke(evt));
#else
                        subscription.Invoke(evt);
#endif
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error invoking event handler: {e}");
                    }
                }
                
                // Remove invalid subscriptions
                if (invalidSubscriptions.Count > 0)
                {
                    foreach (var invalid in invalidSubscriptions)
                    {
                        subscribers.Remove(invalid);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error publishing event: {e}");
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            finally
            {
                EventDiagnostics.CompleteTrace(trace, evt);
            }
#endif
        }

        #endregion

        #region Helper

        private static void AddSubscription(WeakEventSubscription subscription)
        {
            ValidateSubscription(subscription);
            if (!_subscriptions.ContainsKey(subscription.EventType))
            {
                _subscriptions[subscription.EventType] = new List<WeakEventSubscription>();
            }

            _subscriptions[subscription.EventType].Add(subscription);
        }
        
        private static void ValidateSubscription(WeakEventSubscription subscription)
        {
            if (subscription.EventType == null)
                throw new ArgumentException("Event type cannot be null");
        
            if (!typeof(GameEvent).IsAssignableFrom(subscription.EventType))
                throw new ArgumentException($"Event type must inherit from GameEvent: {subscription.EventType}");
        
            if (!subscription.IsDynamic && subscription.Method == null)
                throw new ArgumentException("Static subscription requires a valid method");
        }
        
        /// <summary>
        /// Registers a Unity object for automatic cleanup when destroyed.
        /// </summary>
        private static void RegisterForAutomaticCleanup(object subscriber)
        {
            // If it's a MonoBehaviour, we can use OnDestroy to automatically clean up
            if (subscriber is MonoBehaviour monoBehaviour)
            {
                // Check if it already has an EventBusCleanupComponent
                var cleanup = monoBehaviour.GetComponent<EventBusCleanupComponent>();
                if (cleanup == null)
                {
                    // Add the cleanup component
                    cleanup = monoBehaviour.gameObject.AddComponent<EventBusCleanupComponent>();
                    cleanup.hideFlags = HideFlags.HideInInspector;
                    cleanup.Initialize(monoBehaviour);
                }
            }
        }
        
        /// <summary>
        /// Performs garbage collection of stale subscriptions.
        /// </summary>
        public static void CollectGarbage()
        {
            int removedCount = 0;
            
            foreach (var kvp in _subscriptions)
            {
                var list = kvp.Value;
                int initialCount = list.Count;
                
                // Remove invalid subscriptions
                list.RemoveAll(s => !s.IsValid());
                
                removedCount += initialCount - list.Count;
            }
            
            if (removedCount > 0)
            {
                Debug.Log($"EventBus garbage collection removed {removedCount} stale subscriptions");
            }
            
            _lastGcTime = Time.realtimeSinceStartup;
        }
        
        /// <summary>
        /// Checks if garbage collection should be performed and does it if needed.
        /// </summary>
        private static void CheckForGarbageCollection()
        {
            if (!_autoCleanupEnabled || _gcInterval <= 0)
                return;
                
            if (Time.realtimeSinceStartup - _lastGcTime >= _gcInterval)
            {
                CollectGarbage();
            }
        }
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Converts a WeakEventSubscription to an EventSubscription for diagnostics.
        /// </summary>
        private static EventSubscription ConvertToEventSubscription(WeakEventSubscription weakSub)
        {
            return new EventSubscription
            {
                EventType = weakSub.EventType,
                TargetInstance = weakSub.TargetInstance,
                Method = weakSub.Method,
                Priority = weakSub.Priority,
                IsDynamic = weakSub.IsDynamic,
                DynamicCallback = weakSub.DynamicCallback
            };
        }
#endif

        #endregion
    }
}