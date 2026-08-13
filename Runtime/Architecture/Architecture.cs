using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aoiro
{
    public interface IArchitecture
    {
        bool ContainsModule<T>() where T : class;
        void RegisterModule<T>(T module) where T : class, IArchitectureModule;
        void UnregisterModule<T>() where T : class, IArchitectureModule;
        T GetModule<T>() where T : class, IArchitectureModule;
        IEnumerable<TModule> EnumerateModules<TModule>() where TModule : class;

        void ExecuteCommand<TCommand>(TCommand command) where TCommand : ICommand;
        TResult ExecuteCommand<TCommand, TResult>(TCommand command) where TCommand : ICommand<TResult>;
        TResult ExecuteQuery<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>;

        SubscriptionToken SubscribeEvent<TEvent>(Action<TEvent> callback);
        void PublishEvent<TEvent>(in TEvent evt);
    }

    public abstract class Architecture<TArchitecture> : IArchitecture, ICommandContext, IQueryContext where TArchitecture : Architecture<TArchitecture>, new()
    {
        private static TArchitecture _instance;

        private readonly EventBus _events = new EventBus();
        private readonly ModuleRegistry _modules = new ModuleRegistry();
        private readonly HashSet<IArchitectureModule> _initializedModules = new HashSet<IArchitectureModule>();
        private bool _modulesInitialized;

        public static TArchitecture Instance
        {
            get
            {
                Initialize();
                return _instance;
            }
        }

        public static bool IsInitialized => _instance != null && _instance._modulesInitialized;

        public static bool TryGetInstance(out TArchitecture architecture)
        {
            architecture = _instance;
            return architecture != null;
        }

        public static void Initialize()
        {
            if (_instance != null)
            {
                return;
            }

            var architecture = new TArchitecture();
            _instance = architecture;
            try
            {
                architecture.OnInitialize();
                InitializeModules<IService>(architecture);
                InitializeModules<IModel>(architecture);
                InitializeModules<ISystem>(architecture);
                architecture._modulesInitialized = true;
            }
            catch
            {
                architecture.ShutdownCore(true);
                _instance = null;
                throw;
            }
        }

        public static void Shutdown()
        {
            var architecture = _instance;
            if (architecture == null)
            {
                return;
            }

            try
            {
                architecture.ShutdownCore(true);
            }
            finally
            {
                architecture._modulesInitialized = false;
                _instance = null;
            }
        }

        protected abstract void OnInitialize();
        protected abstract void OnShutdown();

        IArchitecture IArchitectureProvider.GetArchitecture() => this;

        public bool ContainsModule<T>() where T : class => _modules.Contains<T>();

        public void RegisterModule<T>(T module) where T : class, IArchitectureModule
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            UnregisterModule<T>();
            _modules.Register(module);
            if (!_modulesInitialized)
            {
                return;
            }

            try
            {
                module.Initialize(this);
                _initializedModules.Add(module);
            }
            catch
            {
                _modules.Unregister<T>(out _);
                throw;
            }
        }

        public void UnregisterModule<T>() where T : class, IArchitectureModule
        {
            if (_modules.Unregister<T>(out var module))
            {
                try
                {
                    if (_initializedModules.Contains(module))
                    {
                        module.Shutdown();
                    }
                }
                finally
                {
                    _initializedModules.Remove(module);
                }
            }
        }

        public virtual T GetModule<T>() where T : class, IArchitectureModule => _modules.Get<T>();

        public IEnumerable<TModule> EnumerateModules<TModule>() where TModule : class => _modules.Enumerate<TModule>();

        public virtual void ExecuteCommand<TCommand>(TCommand command) where TCommand : ICommand => command.Execute(this);

        public virtual TResult ExecuteCommand<TCommand, TResult>(TCommand command) where TCommand : ICommand<TResult> => command.Execute(this);

        public virtual TResult ExecuteQuery<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult> => query.Execute(this);

        public virtual SubscriptionToken SubscribeEvent<TEvent>(Action<TEvent> callback) => _events.Subscribe(callback);

        public virtual void PublishEvent<TEvent>(in TEvent evt) => _events.Publish(evt);

        private static void InitializeModules<TModule>(Architecture<TArchitecture> architecture) where TModule : class, IArchitectureModule
        {
            foreach (var module in architecture._modules.Enumerate<TModule>())
            {
                module.Initialize(architecture);
                architecture._initializedModules.Add(module);
            }
        }

        private void ShutdownCore(bool invokeArchitectureHook)
        {
            if (invokeArchitectureHook)
            {
                TryInvoke(OnShutdown);
            }

            foreach (var module in _modules.EnumerateReverse<IPreShutdownModule>())
            {
                if (module is IArchitectureModule architectureModule && _initializedModules.Contains(architectureModule))
                {
                    TryInvoke(module.PreShutdown);
                }
            }

            ShutdownModules<ISystem>();
            ShutdownModules<IModel>();
            ShutdownModules<IService>();
            _modules.Clear();
            _events.Clear();
        }

        private void ShutdownModules<TModule>() where TModule : class, IArchitectureModule
        {
            foreach (var module in _modules.EnumerateReverse<TModule>())
            {
                if (!_initializedModules.Remove(module))
                {
                    continue;
                }
                TryInvoke(module.Shutdown);
            }
        }

        private static void TryInvoke(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
