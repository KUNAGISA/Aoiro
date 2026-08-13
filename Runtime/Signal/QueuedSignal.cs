using System;
using System.Collections.Generic;

namespace Aoiro
{
    public sealed class QueuedSignal<T> : ISignal
    {
        private const int MaxDispatchCount = 1024;

        private readonly Signal<T> _signal = new Signal<T>();
        private Queue<T> _pending;
        private bool _isDispatching;
        private int _generation;
        private int _dispatchGeneration;

        public SignalToken Register(Action<T> callback) => _signal.Register(callback);

        public void Emit(in T arg)
        {
            if (_isDispatching)
            {
                if (_generation == _dispatchGeneration)
                {
                    (_pending ??= new Queue<T>()).Enqueue(arg);
                }
                return;
            }

            _isDispatching = true;
            _dispatchGeneration = _generation;
            var dispatchCount = 0;
            try
            {
                Dispatch(arg, ref dispatchCount);
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

        public void Cleanup()
        {
            ++_generation;
            _pending?.Clear();
            _signal.Cleanup();
        }

        void ISignal.Cancel(SignalToken token) => _signal.Cancel(token);

        private void Dispatch(in T arg, ref int dispatchCount)
        {
            if (++dispatchCount > MaxDispatchCount)
            {
                throw new InvalidOperationException($"QueuedSignal exceeded {MaxDispatchCount} dispatches in a single cycle. Possible event loop detected.");
            }
            _signal.Emit(arg);
        }
    }
}
