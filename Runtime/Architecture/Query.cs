namespace Aoiro
{
    public interface IQueryContext : IServiceAccessor, IModelAccessor, ISystemAccessor, IQueryExecutor { }

    public interface IQuery<out TResult>
    {
        protected internal TResult Execute(IQueryContext context);
    }
}
