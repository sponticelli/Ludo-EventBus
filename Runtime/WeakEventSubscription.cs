using System;
using System.Reflection;
using UnityEngine;

namespace Ludo.Core.EventBus
{
    /// <summary>
    /// Event subscription that holds a weak reference to the subscriber.
    /// This helps prevent memory leaks when subscribers are not properly unsubscribed.
    /// </summary>
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
        
        /// <summary>
        /// Gets the target instance if it's still alive, otherwise returns null.
        /// </summary>
        public object TargetInstance => _targetInstanceRef.IsAlive ? _targetInstanceRef.Target : null;
        
        /// <summary>
        /// Gets the dynamic callback if this is a dynamic subscription and the target is still alive.
        /// </summary>
        public Action<GameEvent> DynamicCallback => 
            IsDynamic && _targetInstanceRef.IsAlive ? _dynamicCallback : null;
        
        /// <summary>
        /// Creates a new weak event subscription.
        /// </summary>
        public WeakEventSubscription(Type eventType, object targetInstance, MethodInfo method, 
            SubscriberPriority priority, bool isDynamic, Action<GameEvent> dynamicCallback)
        {
            EventType = eventType;
            _targetInstanceRef = new WeakReference(targetInstance);
            Method = method;
            Priority = priority;
            IsDynamic = isDynamic;
            _dynamicCallback = dynamicCallback;
            
            _targetType = targetInstance.GetType();
            _isUnityObject = targetInstance is UnityEngine.Object;
        }
        
        /// <summary>
        /// Checks if the subscription is still valid (target is alive and not destroyed).
        /// </summary>
        public bool IsValid()
        {
            if (!_targetInstanceRef.IsAlive)
                return false;
                
            var target = _targetInstanceRef.Target;
            
            // For Unity objects, check if they've been destroyed
            if (_isUnityObject)
            {
                var unityObject = target as UnityEngine.Object;
                return unityObject != null;
            }
            
            return true;
        }
        
        /// <summary>
        /// Invokes the subscription with the given event.
        /// </summary>
        public void Invoke(GameEvent evt)
        {
            if (!IsValid())
                return;
                
            var target = _targetInstanceRef.Target;
            
            if (IsDynamic)
            {
                _dynamicCallback(evt);
            }
            else
            {
                Method.Invoke(target, new object[] { evt });
            }
        }
    }
}
