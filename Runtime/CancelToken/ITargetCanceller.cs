namespace Aoiro
{
    public interface ITargetCanceller<T>
    {
        void Cancel(T target);
    }
}
