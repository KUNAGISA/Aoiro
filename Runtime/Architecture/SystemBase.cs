namespace Aoiro
{
    public interface ISystem : IArchitectureModule, ISystemAccessor, IModelAccessor, IServiceAccessor, ICommandExecutor, IQueryExecutor, IEventPublisher, IEventSubscriber { }

    public abstract class SystemBase : ModuleBase, ISystem { }
}
