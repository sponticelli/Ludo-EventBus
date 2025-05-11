namespace Ludo.Core.EventBus
{
    /// <summary>
    /// Represents the base class for any custom game event.
    /// You can add properties, constructors, or methods as needed.
    /// </summary>
    public abstract class GameEvent
    {
        // Indicates whether this event has been canceled by a subscriber.
        public bool IsCanceled { get; private set; }

        /// <summary>
        /// Call this to prevent further subscribers (of lower priority) from handling the event.
        /// </summary>
        public void StopPropagation()
        {
            IsCanceled = true;
        }
    }
}