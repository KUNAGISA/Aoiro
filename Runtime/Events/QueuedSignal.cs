using System;
using System.Collections.Generic;

namespace Aoiro
{
    public sealed class QueuedSignal<T> : IEventChannel
    {
        private const int MaxDispatchCount = 1024;

        private readonly Signal<T> _signal = new Signal<T>();
        private Queue<T> _pending;
        private bool _isDispatching;
        private int _generation;
        private int _dispatchGeneration;

        public SubscriptionToken Subscribe(Action<T> callback) => _signal.Subscribe(callback);

        public void Emit(in T value)
        {
            if (_isDispatching)
            {
                if (_generation == _dispatchGeneration)
                {
                    (_pending ??= new Queue<T>()).Enqueue(value);
                }
                return;
            }

            _isDispatching = true;
            _dispatchGeneration = _generation;
            var dispatchCount = 0;
            try
            {
                Dispatch(value, ref dispatchCount);
                while (_generation == _dispatchGeneration && _pending != null && _pending.Count > 0)
                {
                    var pending = _pending.Dequeue();
                    Dispatch(pending, ref dispatchCount);
                }
            }
            catch
            {
                _pending?.Clear();
                throw;
            }
            finally
            {
                _isDispatching = false;
            }
        }

        public void Clear()
        {
            ++_generation;
            _pending?.Clear();
            _signal.Clear();
        }

        private void Dispatch(in T value, ref int dispatchCount)
        {
            if (++dispatchCount > MaxDispatchCount)
            {
                throw new InvalidOperationException($"QueuedSignal exceeded {MaxDispatchCount} dispatches in a single cycle. Possible event loop detected.");
            }
            _signal.Emit(value);
        }
    }
}
