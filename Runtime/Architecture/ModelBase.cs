namespace Aoiro
{
    public interface IModel : IArchitectureModule, IServiceAccessor, IEventPublisher { }

    public abstract class ModelBase : ModuleBase, IModel { }
}
