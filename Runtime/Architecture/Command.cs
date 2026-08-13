namespace Aoiro
{
    public interface ICommandContext : IServiceAccessor, IModelAccessor, ISystemAccessor, ICommandExecutor, IQueryExecutor, IEventPublisher { }

    public interface ICommand
    {
        protected internal void Execute(ICommandContext context);
    }

    public interface ICommand<out TResult>
    {
        protected internal TResult Execute(ICommandContext context);
    }
}
