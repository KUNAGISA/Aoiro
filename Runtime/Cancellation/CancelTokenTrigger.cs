using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aoiro
{
    internal abstract class CancelTokenTrigger : MonoBehaviour
    {
        private readonly HashSet<ICancelToken> _tokens = new HashSet<ICancelToken>();

        private void Awake() => hideFlags = HideFlags.HideAndDontSave;

        internal void Add(ICancelToken token)
        {
            if (token != null)
            {
                _tokens.Add(token);
            }
        }

        internal void CancelAll()
        {
            List<Exception> exceptions = null;
            foreach (var token in _tokens)
            {
                try
                {
                    token.Cancel();
                }
                catch (Exception exception)
                {
                    (exceptions ??= new List<Exception>()).Add(exception);
                }
            }
            _tokens.Clear();
            if (exceptions != null)
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}
