using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aoiro
{
    public sealed class BindableProperty<T> : IReadOnlyBindableProperty<T>
    {
        private readonly IEqualityComparer<T> _comparer;
        private Signal<T> _changed;
        private T _value;

        public T Value
        {
            get => _value;
            set
            {
                if (_comparer.Equals(value, _value))
                {
                    return;
                }
                _value = value;
                _changed?.Emit(_value);
            }
        }

        public BindableProperty(T value = default, IEqualityComparer<T> comparer = null)
        {
            _value = value;
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        public void SetValueWithoutNotify(T value) => _value = value;

        public SubscriptionToken Subscribe(Action<T> callback, bool invokeImmediately = false)
        {
            if (callback == null)
            {
                return default;
            }
            if (invokeImmediately)
            {
                try
                {
                    callback(_value);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
            _changed ??= new Signal<T>();
            return _changed.Subscribe(callback);
        }

        public static implicit operator T(BindableProperty<T> property) => property._value;
    }
}
