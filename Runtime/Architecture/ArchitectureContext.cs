namespace Aoiro
{
    public abstract class ArchitectureContext : IArchitectureProvider
    {
        private IArchitecture _architecture;

        internal void Bind(IArchitecture architecture) => _architecture = architecture;

        internal void Unbind() => _architecture = null;

        IArchitecture IArchitectureProvider.GetArchitecture() => _architecture;
    }
}
