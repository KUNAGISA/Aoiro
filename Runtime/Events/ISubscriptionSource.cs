namespace Aoiro
{
    internal interface ISubscriptionSource
    {
        void Cancel(SubscriptionToken token);
    }
}
