using System;
using System.Collections.Generic;

namespace Aoiro
{
    internal sealed class ModuleRegistry
    {
        private readonly Dictionary<Type, object> _modules = new Dictionary<Type, object>();
        private readonly List<Type> _registrationOrder = new List<Type>();

        internal bool Contains<T>() => _modules.ContainsKey(typeof(T));

        internal void Register<T>(T module)
        {
            var key = typeof(T);
            if (!_modules.ContainsKey(key))
            {
                _registrationOrder.Add(key);
            }
            _modules[key] = module;
        }

        internal bool Unregister<T>(out T module) where T : class
        {
            var key = typeof(T);
            if (_modules.Remove(key, out var value))
            {
                _registrationOrder.Remove(key);
                module = (T)value;
                return true;
            }

            module = null;
            return false;
        }

        internal T Get<T>() where T : class => _modules.TryGetValue(typeof(T), out var module) ? (T)module : null;

        internal IEnumerable<T> Enumerate<T>() where T : class
        {
            for (var index = 0; index < _registrationOrder.Count; ++index)
            {
                if (_modules[_registrationOrder[index]] is T module)
                {
                    yield return module;
                }
            }
        }

        internal IEnumerable<T> EnumerateReverse<T>() where T : class
        {
            for (var index = _registrationOrder.Count - 1; index >= 0; --index)
            {
                if (_modules[_registrationOrder[index]] is T module)
                {
                    yield return module;
                }
            }
        }

        internal void Clear()
        {
            _modules.Clear();
            _registrationOrder.Clear();
        }
    }
}
