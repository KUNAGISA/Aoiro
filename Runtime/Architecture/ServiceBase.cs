namespace Aoiro
{
    public interface IService : IArchitectureModule, IEventPublisher { }

    public abstract class ServiceBase : ModuleBase, IService { }
}
