using System;

namespace Aoiro
{
    public interface IReadOnlyBindableProperty<T>
    {
        T Value { get; }
        SubscriptionToken Subscribe(Action<T> callback, bool invokeImmediately = false);
    }
}
