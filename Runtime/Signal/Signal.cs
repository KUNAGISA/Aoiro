using System;

namespace Aoiro
{
    internal interface ISignal
    {
        void Cancel(SignalToken token);
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

        private struct EmitContext
        {
            internal int Cursor;
            internal int End;
        }
        
        private int _count = 0;
        private SignalSlot[] _slots = null;

        private EmitContext[] _emitContexts = new EmitContext[1];
        private int _emitDepth;

        private int _activeHead = InvalidIndex;
        private int _activeTail = InvalidIndex;

        private int _freeHead = InvalidIndex;
        private int _retiredHead = InvalidIndex;

        protected AbstractSignal(int capacity = 2)
        {
            _slots = new SignalSlot[capacity > 0 ? capacity : 2];
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
            _retiredHead = InvalidIndex;
            _activeHead = InvalidIndex;
            _activeTail = InvalidIndex;
            for (var index = 0; index < _emitDepth; ++index)
            {
                _emitContexts[index].Cursor = InvalidIndex;
                _emitContexts[index].End = InvalidIndex;
            }
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
            for (var index = 0; index < _emitDepth; ++index)
            {
                ref var context = ref _emitContexts[index];
                if (context.Cursor == token.Index)
                {
                    context.Cursor = nextIndex;
                }
                if (context.End == token.Index)
                {
                    context.End = prevIndex;
                }
            }
            
            slot.Invalidate();
            slot.PrevIndex = InvalidIndex;
            if (_emitDepth == 0)
            {
                slot.NextIndex = _freeHead;
                _freeHead = token.Index;
            }
            else
            {
                slot.NextIndex = _retiredHead;
                _retiredHead = token.Index;
            }
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
        
        protected int BeginEmit()
        {
            if (_emitDepth == _emitContexts.Length)
            {
                Array.Resize(ref _emitContexts, _emitContexts.Length << 1);
            }

            var contextIndex = _emitDepth++;
            _emitContexts[contextIndex].Cursor = _activeHead;
            _emitContexts[contextIndex].End = _activeTail;
            return contextIndex;
        }

        protected bool MoveNext(int contextIndex, out int current)
        {
            ref var context = ref _emitContexts[contextIndex];
            current = context.Cursor;
            if (current == InvalidIndex)
            {
                return false;
            }
            context.Cursor = _slots[current].NextIndex;
            return true;
        }

        protected bool IsLast(int contextIndex, int current)
        {
            return current == _emitContexts[contextIndex].End;
        }

        protected void EndEmit(int contextIndex)
        {
            if (contextIndex != _emitDepth - 1)
            {
                throw new InvalidOperationException("Signal emit contexts must end in stack order.");
            }

            _emitContexts[contextIndex].Cursor = InvalidIndex;
            _emitContexts[contextIndex].End = InvalidIndex;
            --_emitDepth;
            if (_emitDepth == 0)
            {
                RecycleRetiredSlots();
            }
        }

        private void RecycleRetiredSlots()
        {
            while (_retiredHead != InvalidIndex)
            {
                var index = _retiredHead;
                _retiredHead = _slots[index].NextIndex;
                _slots[index].NextIndex = _freeHead;
                _freeHead = index;
            }
        }
    }

    public sealed class Signal : AbstractSignal<Action>
    {
        public void Emit()
        {
            var contextIndex = BeginEmit();
            try
            {
                while (MoveNext(contextIndex, out var current))
                {
                    ref readonly var slot = ref GetSlot(current);
                    slot.Callback?.Invoke(); //有错误直接抛出，让外部调用者自己捕获
                    if (IsLast(contextIndex, current))
                    {
                        break;
                    }
                }
            }
            finally
            {
                EndEmit(contextIndex);
            }
        }
    }
    
    public sealed class Signal<T> : AbstractSignal<Action<T>>
    {
        public void Emit(in T arg)
        {
            var contextIndex = BeginEmit();
            try
            {
                while (MoveNext(contextIndex, out var current))
                {
                    ref readonly var slot = ref GetSlot(current);
                    slot.Callback?.Invoke(arg); //有错误直接抛出，让外部调用者自己捕获
                    if (IsLast(contextIndex, current))
                    {
                        break;
                    }
                }
            }
            finally
            {
                EndEmit(contextIndex);
            }
        }
    }
    
    public sealed class Signal<T1, T2> : AbstractSignal<Action<T1, T2>>
    {
        public void Emit(in T1 arg1, in T2 arg2)
        {
            var contextIndex = BeginEmit();
            try
            {
                while (MoveNext(contextIndex, out var current))
                {
                    ref readonly var slot = ref GetSlot(current);
                    slot.Callback?.Invoke(arg1, arg2); //有错误直接抛出，让外部调用者自己捕获
                    if (IsLast(contextIndex, current))
                    {
                        break;
                    }
                }
            }
            finally
            {
                EndEmit(contextIndex);
            }
        }
    }
}
