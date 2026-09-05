using System;

namespace Aoiro
{
    public interface ISignal
    {
        SubscriptionToken Subscribe(Action callback);
    }

    public interface ISignal<T>
    {
        SubscriptionToken Subscribe(Action<T> callback);
    }
}
