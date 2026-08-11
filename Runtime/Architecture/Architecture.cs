using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Aoiro
{
    public partial interface IArchitecture
    {
        bool Contains<T>() where T : class;
        void Register<T>(T instance) where T : class, IArchitectureModule;
        void UnRegister<T>() where T : class, IArchitectureModule;
        T Get<T>() where T : class, IArchitectureModule;

        void SendCommand<TCommand>(TCommand command) where TCommand : ICommand;

        TResult SendCommand<TCommand, TResult>(TCommand command) where TCommand : ICommand<TResult>;

        TResult SendQuery<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>;

        SignalToken RegisterEvent<TEvent>(Action<TEvent> onEvent);
        void SendEvent<TEvent>(in TEvent evt);
    }

    public abstract partial class Architecture<TArchitecture> : IArchitecture, ICommandContext, IQueryContext where TArchitecture : Architecture<TArchitecture>, new()
    {
        public static event Action<TArchitecture> OnRegisterPatch;

        private static TArchitecture _architecture = null;
        public static TArchitecture Instance
        {
            get
            {
                if (_architecture == null)
                {
                    MakeSureArchitecture();
                }
                return _architecture;
            }
        }

        public static bool Valid => _architecture != null;

        public static bool TryGetInstance(out TArchitecture architecture)
        {
            architecture = _architecture;
            return architecture != null;
        }

        public static void MakeSureArchitecture()
        {
            if (_architecture != null)
            {
                return;
            }

            _architecture = new TArchitecture();
            _architecture.OnInit();

            OnRegisterPatch?.Invoke(_architecture);

            InitModules<IService>(_architecture._iocContainer);
            InitModules<IModel>(_architecture._iocContainer);
            InitModules<ISystem>(_architecture._iocContainer);

            _architecture._initialize = true;
        }

        public static void DestroyInstance()
        {
            if (_architecture == null)
            {
                return;
            }

            try
            {
                try
                {
                    _architecture.OnDestroy();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }

                PreShutdownModules(_architecture._iocContainer);
                DestroyModules<ISystem>(_architecture._iocContainer);
                DestroyModules<IModel>(_architecture._iocContainer);
                DestroyModules<IService>(_architecture._iocContainer);

                _architecture._iocContainer.Clear();
                _architecture._events.Clear();
            }
            finally
            {
                _architecture._initialize = false;
                _architecture = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PreShutdownModules(IOCContainer ioc)
        {
            foreach (var module in ioc.OfTypeReverse<IArchitecturePreShutdown>())
            {
                try
                {
                    module.PreShutdown();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InitModules<T>(IOCContainer ioc) where T : class, IArchitectureModule
        {
            foreach(var module in ioc.OfType<T>())
            {
                module.Init();
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DestroyModules<T>(IOCContainer ioc) where T : class, IArchitectureModule
        {
            foreach(var module in ioc.OfTypeReverse<T>())
            {
                try
                {
                    module.Destroy();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
                module.SetArchitecture(null);
            }
        }
        
        private bool _initialize = false;
        private readonly EventBus _events = new EventBus();
        private readonly IOCContainer _iocContainer = new IOCContainer();

        protected abstract void OnInit();
        protected abstract void OnDestroy();

        IArchitecture IBelongArchitecture.GetArchitecture() => this;

        public bool Contains<T>() where T : class
        {
            return _iocContainer.Contains<T>();
        }

        public void Register<T>(T instance) where T : class, IArchitectureModule
        {
            UnRegister<T>();

            instance.SetArchitecture(this);
            _iocContainer.Register(instance);

            if (_initialize)
            {
                instance.Init();
            }
        }

        public void UnRegister<T>() where T : class, IArchitectureModule
        {
            if (_iocContainer.UnRegister<T>(out var instance))
            {
                instance.Destroy();
                instance.SetArchitecture(null);
            }
        }

        public virtual T Get<T>() where T : class, IArchitectureModule
        {
            return _iocContainer.Get<T>();
        }
        
        public virtual void SendCommand<TCommand>(TCommand command) where TCommand : ICommand
        {
            command.Execute(this);
        }

        public virtual TResult SendCommand<TCommand, TResult>(TCommand command) where TCommand : ICommand<TResult>
        {
            return command.Execute(this);
        }

        public virtual TResult SendQuery<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
        {
            return query.Do(this);
        }

        public virtual SignalToken RegisterEvent<TEvent>(Action<TEvent> onEvent)
        {
            return _events.Register(onEvent);
        }
        
        public virtual void SendEvent<TEvent>(in TEvent evt)
        {
            _events.Send(evt);
        }
    }
}
