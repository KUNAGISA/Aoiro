using System;

namespace Aoiro
{
    internal interface ISignal : ITargetCanceller<SignalToken>
    {
        void Cleanup();
    }

    public abstract class AbstractSignal<T> : ISignal where T : Delegate
    {
        private const int InvalidIndex = -1;

        protected struct SignalSlot
        {
            internal int PrevIndex;
            internal int NextIndex;
            internal int Version;

            internal T Callback;
        
            internal void Invalidate()
            {
                ++Version;
                Callback = null;
            }
        }
        
        private int _count = 0;
        private SignalSlot[] _slots = null;

        private int _emitCursor = InvalidIndex;
        private int _emitEnd = InvalidIndex;

        private int _activeHead = InvalidIndex;
        private int _activeTail = InvalidIndex;

        private int _freeHead = InvalidIndex;

        protected AbstractSignal(int capacity = 8)
        {
            _slots = new SignalSlot[capacity > 0 ? capacity : 4];
        }
        
        public void Cleanup()
        {
            for (var index = 0; index < _count; ++index)
            {
                _slots[index].Invalidate();
                _slots[index].PrevIndex = _slots[index].NextIndex = InvalidIndex;
            }
            _count = 0;
            _freeHead = InvalidIndex;
            _activeHead = InvalidIndex;
            _activeTail = InvalidIndex;
            _emitCursor = InvalidIndex;
            _emitEnd = InvalidIndex;
        }
        
        public SignalToken Register(T callback)
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
                _freeHead = _slots[_freeHead].NextIndex;
            }

            ref var slot = ref _slots[index];
            slot.Callback = callback;

            if (_activeHead == InvalidIndex)
            {
                _activeHead = index;
                _activeTail = index;
                slot.PrevIndex = InvalidIndex;
                slot.NextIndex = InvalidIndex;
            }
            else
            {
                // 能跑进来_activeTail必定是有效的
                _slots[_activeTail].NextIndex = index;
                slot.PrevIndex = _activeTail;
                slot.NextIndex = InvalidIndex;
                _activeTail = index;
            }

            return new SignalToken(this, index, slot.Version);
        }
        
        public void Cancel(SignalToken token)
        {
            if (token.Signal != this || token.Index >= _count || token.Index < 0)
            {
                return;
            }

            if (_slots[token.Index].Version != token.Version)
            {
                return;
            }

            ref var slot = ref _slots[token.Index];
            var prevIndex = slot.PrevIndex;
            var nextIndex = slot.NextIndex;

            if (prevIndex != InvalidIndex)
            {
                _slots[prevIndex].NextIndex = nextIndex;
            }
            if (nextIndex != InvalidIndex)
            {
                _slots[nextIndex].PrevIndex = prevIndex;
            }
            if (_activeHead == token.Index)
            {
                _activeHead = nextIndex;
            }
            if (_activeTail == token.Index)
            {
                _activeTail = prevIndex;
            }
            if (_emitCursor == token.Index)
            {
                _emitCursor = nextIndex;
            }
            if (_emitEnd == token.Index)
            {
                _emitEnd = prevIndex;
            }
            
            slot.Invalidate();
            slot.PrevIndex = InvalidIndex;
            slot.NextIndex = _freeHead;
            _freeHead = token.Index;
        }
        
        private void EnsureCapacity()
        {
            if (_count == _slots.Length)
            {
                Array.Resize(ref _slots, _slots.Length << 1);
            }
        }

        protected ref readonly SignalSlot GetSlot(int index)
        {
            return ref _slots[index];
        }
        
        protected void BeginEmit()
        {
            if (_activeHead == InvalidIndex)
            {
                _emitCursor = InvalidIndex;
                _emitEnd = InvalidIndex;
                return;
            }

            _emitCursor = _activeHead;
            _emitEnd = _activeTail;
        }

        protected bool MoveNext(out int current)
        {
            current = _emitCursor;
            if (current == InvalidIndex)
            {
                return false;
            }
            _emitCursor = _slots[current].NextIndex;
            return true;
        }

        protected bool IsLast(int current)
        {
            return current == _emitEnd;
        }

        protected void EndEmit()
        {
            _emitCursor = InvalidIndex;
            _emitEnd = InvalidIndex;
        }
    }

    public sealed class Signal : AbstractSignal<Action>
    {
        public void Emit()
        {
            BeginEmit();
            try
            {
                while (MoveNext(out var current))
                {
                    ref readonly var slot = ref GetSlot(current);
                    slot.Callback?.Invoke(); //有错误直接抛出，让外部调用者自己捕获
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
    
    public sealed class Signal<T> : AbstractSignal<Action<T>>
    {
        public void Emit(in T arg)
        {
            BeginEmit();
            try
            {
                while (MoveNext(out var current))
                {
                    ref readonly var slot = ref GetSlot(current);
                    slot.Callback?.Invoke(arg); //有错误直接抛出，让外部调用者自己捕获
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
    
    public sealed class Signal<T1, T2> : AbstractSignal<Action<T1, T2>>
    {
        public void Emit(in T1 arg1, in T2 arg2)
        {
            BeginEmit();
            try
            {
                while (MoveNext(out var current))
                {
                    ref readonly var slot = ref GetSlot(current);
                    slot.Callback?.Invoke(arg1, arg2); //有错误直接抛出，让外部调用者自己捕获
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
