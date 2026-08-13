using System;
using System.Collections.Generic;

namespace Aoiro
{
    public sealed class EventBus
    {
        private readonly Dictionary<Type, ISignal> _signals = new();

        public void Send<T>(in T e)
        {
            if (_signals.TryGetValue(typeof(QueuedSignal<T>), out var signal))
            {
                ((QueuedSignal<T>)signal).Emit(e);
            }
        }

        public SignalToken Register<T>(Action<T> callback)
        {
            var key = typeof(QueuedSignal<T>);
            if (!_signals.TryGetValue(key, out var signal))
            {
                signal = new QueuedSignal<T>();
                _signals.Add(key, signal);
            }
            return ((QueuedSignal<T>)signal).Register(callback);
        }
        
        public void Clear()
        {
            foreach (var (_, signal) in _signals)
            {
                signal.Cleanup();
            }
            _signals.Clear();
        }
    }
}
