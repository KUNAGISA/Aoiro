using System;

namespace Aoiro
{
    public interface IBelongArchitecture
    {
        protected internal IArchitecture GetArchitecture();
    }

    public interface IArchitectureModule
    {
        protected internal void SetArchitecture(IArchitecture architecture);
        protected internal void Init();
        protected internal void Destroy();
    }

    public interface IArchitecturePreShutdown
    {
        protected internal void PreShutdown();
    }

    public interface ICanGetService : IBelongArchitecture { }

    public interface ICanGetModel : IBelongArchitecture { }

    public interface ICanGetSystem : IBelongArchitecture { }

    public interface ICanSendCommand : IBelongArchitecture { }

    public interface ICanSendQuery : IBelongArchitecture { }

    public interface ICanRegisterEvent : IBelongArchitecture { }

    public interface ICanSendEvent : IBelongArchitecture { }

    public static class ArchitectureExtensions
    {
        public static TUtility GetService<TUtility>(this ICanGetService context) where TUtility : class, IService
        {
            return context.GetArchitecture().Get<TUtility>();
        }
        
        public static TModel GetModel<TModel>(this ICanGetModel context) where TModel : class, IModel
        {
            return context.GetArchitecture().Get<TModel>();
        }
        
        public static TSystem GetSystem<TSystem>(this ICanGetSystem context) where TSystem : class, ISystem
        {
            return context.GetArchitecture().Get<TSystem>();
        }
        
        public static void SendCommand(this ICanSendCommand context, ICommand command)
        {
            context.GetArchitecture().SendCommand(command);
        }

        public static void SendCommand<TCommand>(this ICanSendCommand context, TCommand command) where TCommand : ICommand
        {
            context.GetArchitecture().SendCommand(command);
        }

        public static TResult SendCommand<TResult>(this ICanSendCommand context, ICommand<TResult> command)
        {
            return context.GetArchitecture().SendCommand<ICommand<TResult>, TResult>(command);
        }

        public static TResult SendCommand<TCommand, TResult>(this ICanSendCommand context, TCommand command) where TCommand : struct, ICommand<TResult>
        {
            return context.GetArchitecture().SendCommand<TCommand, TResult>(command);
        }

        public static TResult SendQuery<TResult>(this ICanSendQuery context, IQuery<TResult> command)
        {
            return context.GetArchitecture().SendQuery<IQuery<TResult>, TResult>(command);
        }

        public static TResult SendQuery<TQuery, TResult>(this ICanSendQuery context, TQuery query) where TQuery : struct, IQuery<TResult>
        {
            return context.GetArchitecture().SendQuery<TQuery, TResult>(query);
        }

        public static SignalToken RegisterEvent<TEvent>(this ICanRegisterEvent context, Action<TEvent> onEvent)
        {
            return context.GetArchitecture().RegisterEvent(onEvent);
        }
        
        public static void SendEvent<TEvent>(this ICanSendEvent context) where TEvent : new()
        {
            context.GetArchitecture().SendEvent(new TEvent());
        }

        public static void SendEvent<TEvent>(this ICanSendEvent context, in TEvent evt)
        {
            context.GetArchitecture().SendEvent(evt);
        }
    }
}
