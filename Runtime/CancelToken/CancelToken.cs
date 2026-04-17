using System;

namespace Aoiro
{
    public static class CancelToken
    {
        public static ICancelToken None { get; } = new NoneCancelToken();

        public static ICancelToken FromCallback(Action callback)
        {
            return callback != null ? new CallbackCancelToken(callback) : None;
        }

        public static ICancelToken FromTarget<T>(ITargetCanceller<T> canceller, T target)
        {
            return new TargetCancelToken<T>(canceller, target);
        }
    }

    internal sealed class NoneCancelToken : ICancelToken
    {
        public void Cancel() { }
    }
    
    internal sealed class CallbackCancelToken : ICancelToken
    {
        private Action _callback = null;

        public CallbackCancelToken(Action callback)
        {
            _callback = callback;
        }

        public void Cancel()
        {
            _callback?.Invoke();
            _callback = null;
        }
    }

    internal sealed class TargetCancelToken<T> : ICancelToken
    {
        private ITargetCanceller<T> _canceller = null;
        private T _target = default;

        public TargetCancelToken(ITargetCanceller<T> canceller, T target)
        {
            _canceller = canceller;
            _target = target;
        }

        public void Cancel()
        {
            _canceller?.Cancel(_target);
            _canceller = null;
            _target = default;
        }
    }
}