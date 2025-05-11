using UnityEngine;

namespace Ludo.Core.EventBus
{
    /// <summary>
    /// Component that automatically cleans up event subscriptions when a GameObject is destroyed.
    /// This is added automatically to GameObjects that subscribe to events.
    /// </summary>
    [AddComponentMenu("")] // Hide from Add Component menu
    public class EventBusCleanupComponent : MonoBehaviour
    {
        private MonoBehaviour _target;
        
        /// <summary>
        /// Initialize the cleanup component with the target MonoBehaviour.
        /// </summary>
        public void Initialize(MonoBehaviour target)
        {
            _target = target;
        }
        
        private void OnDestroy()
        {
            if (_target != null)
            {
                // Unbind all static subscriptions
                GameEventHub.Unbind(_target);
                
                // Dynamic subscriptions should be unsubscribed in the target's OnDestroy method
                // using the unsubscribe action returned by Listen<T>()
            }
        }
    }
}