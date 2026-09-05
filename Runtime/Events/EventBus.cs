using System;
using System.Collections.Generic;

namespace Aoiro
{
    public sealed class EventBus
    {
        private readonly Dictionary<Type, IEventChannel> _channels = new Dictionary<Type, IEventChannel>();

        public void Publish<T>(in T evt)
        {
            if (_channels.TryGetValue(typeof(T), out var channel))
            {
                ((QueuedSignal<T>)channel).Emit(evt);
            }
        }

        public SubscriptionToken Subscribe<T>(Action<T> callback)
        {
            var key = typeof(T);
            if (!_channels.TryGetValue(key, out var channel))
            {
                channel = new QueuedSignal<T>();
                _channels.Add(key, channel);
            }
            return ((QueuedSignal<T>)channel).Subscribe(callback);
        }

        public void Clear()
        {
            foreach (var channel in _channels.Values)
            {
                channel.Reset();
            }
            _channels.Clear();
        }
    }
}
