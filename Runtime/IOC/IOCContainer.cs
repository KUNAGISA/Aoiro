using System;
using System.Collections.Generic;

namespace Aoiro
{
    public sealed class IOCContainer
    {
        private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        private readonly List<Type> _registrationOrder = new List<Type>();

        public bool Contains<T>()
        {
            return _instances.ContainsKey(typeof(T));
        }

        public void Register<T>(T instance)
        {
            var key = typeof(T);
            if (!_instances.ContainsKey(key))
            {
                _registrationOrder.Add(key);
            }
            _instances[key] = instance;
        }

        public bool UnRegister<T>(out T instance) where T : class
        {
            instance = null;
            if (_instances.Remove(typeof(T), out var value))
            {
                _registrationOrder.Remove(typeof(T));
                instance = (T)value;
            }
            return instance != null;
        }

        public T Get<T>() where T : class
        {
            return _instances.TryGetValue(typeof(T), out var instance) ? (T)instance : null;
        }

        public IEnumerable<T> OfType<T>() where T : class
        {
            for (var index = 0; index < _registrationOrder.Count; ++index)
            {
                if (_instances[_registrationOrder[index]] is T instance)
                {
                    yield return instance;
                }
            }
        }

        public IEnumerable<T> OfTypeReverse<T>() where T : class
        {
            for (var index = _registrationOrder.Count - 1; index >= 0; --index)
            {
                if (_instances[_registrationOrder[index]] is T instance)
                {
                    yield return instance;
                }
            }
        }

        public void Clear()
        {
            _instances.Clear();
            _registrationOrder.Clear();
        }
    }
}
