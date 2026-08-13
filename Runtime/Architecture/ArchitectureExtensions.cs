using System;

namespace Aoiro
{
    public interface IArchitectureProvider
    {
        protected internal IArchitecture GetArchitecture();
    }

    public interface IArchitectureModule
    {
        protected internal void Initialize(IArchitecture architecture);
        protected internal void Shutdown();
    }

    public interface IPreShutdownModule
    {
        protected internal void PreShutdown();
    }

    public interface IServiceAccessor : IArchitectureProvider { }
    public interface IModelAccessor : IArchitectureProvider { }
    public interface ISystemAccessor : IArchitectureProvider { }
    public interface ICommandExecutor : IArchitectureProvider { }
    public interface IQueryExecutor : IArchitectureProvider { }
    public interface IEventSubscriber : IArchitectureProvider { }
    public interface IEventPublisher : IArchitectureProvider { }

    public static class ArchitectureExtensions
    {
        public static TService GetService<TService>(this IServiceAccessor context) where TService : class, IService => context.GetArchitecture().GetModule<TService>();

        public static TModel GetModel<TModel>(this IModelAccessor context) where TModel : class, IModel => context.GetArchitecture().GetModule<TModel>();

        public static TSystem GetSystem<TSystem>(this ISystemAccessor context) where TSystem : class, ISystem => context.GetArchitecture().GetModule<TSystem>();

        public static void ExecuteCommand(this ICommandExecutor context, ICommand command) => context.GetArchitecture().ExecuteCommand(command);

        public static void ExecuteCommand<TCommand>(this ICommandExecutor context, TCommand command) where TCommand : ICommand => context.GetArchitecture().ExecuteCommand(command);

        public static TResult ExecuteCommand<TResult>(this ICommandExecutor context, ICommand<TResult> command) => context.GetArchitecture().ExecuteCommand<ICommand<TResult>, TResult>(command);

        public static TResult ExecuteCommand<TCommand, TResult>(this ICommandExecutor context, TCommand command) where TCommand : struct, ICommand<TResult> => context.GetArchitecture().ExecuteCommand<TCommand, TResult>(command);

        public static TResult ExecuteQuery<TResult>(this IQueryExecutor context, IQuery<TResult> query) => context.GetArchitecture().ExecuteQuery<IQuery<TResult>, TResult>(query);

        public static TResult ExecuteQuery<TQuery, TResult>(this IQueryExecutor context, TQuery query) where TQuery : struct, IQuery<TResult> => context.GetArchitecture().ExecuteQuery<TQuery, TResult>(query);

        public static SubscriptionToken SubscribeEvent<TEvent>(this IEventSubscriber context, Action<TEvent> callback) => context.GetArchitecture().SubscribeEvent(callback);

        public static void PublishEvent<TEvent>(this IEventPublisher context) where TEvent : new() => context.GetArchitecture().PublishEvent(new TEvent());

        public static void PublishEvent<TEvent>(this IEventPublisher context, in TEvent evt) => context.GetArchitecture().PublishEvent(evt);
    }
}
