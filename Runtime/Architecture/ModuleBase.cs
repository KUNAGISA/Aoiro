namespace Aoiro
{
    public abstract class ModuleBase : IArchitectureModule, IArchitectureProvider
    {
        private IArchitecture _architecture;
        private System.Collections.Generic.List<ArchitectureContext> _contexts;

        protected virtual void OnInitialize() { }
        protected virtual void OnShutdown() { }

        protected TContext BindContext<TContext>(TContext context) where TContext : ArchitectureContext
        {
            if (context == null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }
            context.Bind(_architecture);
            (_contexts ??= new System.Collections.Generic.List<ArchitectureContext>()).Add(context);
            return context;
        }

        protected void UnbindContext(ArchitectureContext context)
        {
            if (context != null && _contexts != null && _contexts.Remove(context))
            {
                context.Unbind();
            }
        }

        void IArchitectureModule.Initialize(IArchitecture architecture)
        {
            _architecture = architecture;
            try
            {
                OnInitialize();
            }
            catch
            {
                UnbindContexts();
                _architecture = null;
                throw;
            }
        }

        void IArchitectureModule.Shutdown()
        {
            try
            {
                OnShutdown();
            }
            finally
            {
                UnbindContexts();
                _architecture = null;
            }
        }

        IArchitecture IArchitectureProvider.GetArchitecture() => _architecture;

        private void UnbindContexts()
        {
            if (_contexts == null)
            {
                return;
            }
            for (var index = 0; index < _contexts.Count; ++index)
            {
                _contexts[index].Unbind();
            }
            _contexts.Clear();
        }
    }
}
