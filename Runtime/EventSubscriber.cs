using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ludo.Core.EventBus
{
    /// <summary>
    /// Base class for MonoBehaviours that subscribe to events.
    /// Automatically handles binding and unbinding in the MonoBehaviour lifecycle.
    /// </summary>
    public abstract class EventSubscriber : MonoBehaviour
    {
        // List of dynamic subscriptions to clean up
        private readonly List<Action> _unsubscribers = new List<Action>();
        
        protected virtual void Awake()
        {
            // Bind all methods with [OnGameEvent] attribute
            GameEventHub.Bind(this);
        }
        
        protected virtual void OnDestroy()
        {
            // Unbind all methods with [OnGameEvent] attribute
            GameEventHub.Unbind(this);
            
            // Invoke all unsubscribe actions for dynamic subscriptions
            foreach (var unsubscribe in _unsubscribers)
            {
                unsubscribe?.Invoke();
            }
            
            _unsubscribers.Clear();
        }
        
        /// <summary>
        /// Subscribe to an event with a callback method.
        /// The subscription is automatically cleaned up when the GameObject is destroyed.
        /// </summary>
        protected Action<TEvent> Subscribe<TEvent>(Action<TEvent> callback, 
            SubscriberPriority priority = SubscriberPriority.Medium) 
            where TEvent : GameEvent
        {
            var unsubscribe = GameEventHub.Listen(this, callback, priority);
            _unsubscribers.Add(unsubscribe);
            
            return callback;
        }
        
        /// <summary>
        /// Unsubscribe from an event.
        /// </summary>
        protected void Unsubscribe(Action unsubscribe)
        {
            if (unsubscribe != null && _unsubscribers.Contains(unsubscribe))
            {
                unsubscribe.Invoke();
                _unsubscribers.Remove(unsubscribe);
            }
        }
    }
}