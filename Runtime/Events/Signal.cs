using System;
using System.Runtime.CompilerServices;

namespace Aoiro
{
    public abstract class SignalBase<TDelegate> : ISubscriptionSource where TDelegate : Delegate
    {
        private const int InvalidIndex = -1;

        protected struct Slot
        {
            internal int Previous;
            internal int Next;
            internal int Version;
            internal TDelegate Callback;

            internal void Invalidate()
            {
                ++Version;
                Callback = null;
            }
        }

        private Slot[] _slots;
        private int _count;
        private int _activeHead = InvalidIndex;
        private int _activeTail = InvalidIndex;
        private int _freeHead = InvalidIndex;
        private bool _isEmitting;
        private int _emitCursor = InvalidIndex;
        private int _emitEnd = InvalidIndex;

        public SubscriptionToken Subscribe(TDelegate callback)
        {
            if (callback == null)
            {
                return default;
            }

            int index;
            if (_freeHead == InvalidIndex)
            {
                EnsureCapacity();
                index = _count++;
            }
            else
            {
                index = _freeHead;
                _freeHead = _slots[index].Next;
            }

            ref var slot = ref _slots[index];
            slot.Callback = callback;
            slot.Previous = _activeTail;
            slot.Next = InvalidIndex;
            if (_activeTail == InvalidIndex)
            {
                _activeHead = index;
            }
            else
            {
                _slots[_activeTail].Next = index;
            }
            _activeTail = index;
            return new SubscriptionToken(this, index, slot.Version);
        }

        public void Clear()
        {
            for (var index = 0; index < _count; ++index)
            {
                _slots[index].Invalidate();
                _slots[index].Previous = InvalidIndex;
                _slots[index].Next = InvalidIndex;
            }

            _count = 0;
            _activeHead = InvalidIndex;
            _activeTail = InvalidIndex;
            _freeHead = InvalidIndex;
            _emitCursor = InvalidIndex;
            _emitEnd = InvalidIndex;
        }

        void ISubscriptionSource.Cancel(SubscriptionToken token)
        {
            if (token.Source != this || token.Index < 0 || token.Index >= _count || _slots[token.Index].Version != token.Version)
            {
                return;
            }

            ref var slot = ref _slots[token.Index];
            var previous = slot.Previous;
            var next = slot.Next;
            if (previous == InvalidIndex)
            {
                _activeHead = next;
            }
            else
            {
                _slots[previous].Next = next;
            }
            if (next == InvalidIndex)
            {
                _activeTail = previous;
            }
            else
            {
                _slots[next].Previous = previous;
            }
            if (_isEmitting)
            {
                if (_emitCursor == token.Index)
                {
                    _emitCursor = next;
                }
                if (_emitEnd == token.Index)
                {
                    _emitEnd = previous;
                }
            }

            slot.Invalidate();
            slot.Previous = InvalidIndex;
            slot.Next = _freeHead;
            _freeHead = token.Index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool CanSkipEmit() => _activeHead == InvalidIndex && !_isEmitting;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void BeginEmit()
        {
            if (_isEmitting)
            {
                throw new InvalidOperationException("Recursive emission of the same Signal instance is not supported.");
            }
            _isEmitting = true;
            _emitCursor = _activeHead;
            _emitEnd = _activeTail;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool MoveNext(out int current)
        {
            current = _emitCursor;
            if (current == InvalidIndex)
            {
                return false;
            }
            _emitCursor = _slots[current].Next;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsLast(int current) => current == _emitEnd;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ref readonly Slot GetSlot(int index) => ref _slots[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void EndEmit()
        {
            _emitCursor = InvalidIndex;
            _emitEnd = InvalidIndex;
            _isEmitting = false;
        }

        private void EnsureCapacity()
        {
            if (_slots == null)
            {
                _slots = new Slot[2];
            }
            else if (_count == _slots.Length)
            {
                Array.Resize(ref _slots, _slots.Length << 1);
            }
        }
    }

    public sealed class Signal : SignalBase<Action>, ISignal
    {
        public void Emit()
        {
            if (CanSkipEmit())
            {
                return;
            }
            BeginEmit();
            try
            {
                while (MoveNext(out var current))
                {
                    GetSlot(current).Callback?.Invoke();
                    if (IsLast(current))
                    {
                        break;
                    }
                }
            }
            finally
            {
                EndEmit();
            }
        }
    }

    public sealed class Signal<T> : SignalBase<Action<T>>, ISignal<T>
    {
        public void Emit(in T value)
        {
            if (CanSkipEmit())
            {
                return;
            }
            BeginEmit();
            try
            {
                while (MoveNext(out var current))
                {
                    GetSlot(current).Callback?.Invoke(value);
                    if (IsLast(current))
                    {
                        break;
                    }
                }
            }
            finally
            {
                EndEmit();
            }
        }
    }
}
