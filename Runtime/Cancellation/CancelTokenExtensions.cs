#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aoiro
{
    public static class CancelTokenExtensions
    {
        public static void CancelOnDestroy(this ICancelToken token, GameObject gameObject)
        {
            if (token == null)
            {
                return;
            }
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }
            if (!gameObject.TryGetComponent<CancelOnDestroyTrigger>(out var trigger))
            {
                trigger = gameObject.AddComponent<CancelOnDestroyTrigger>();
            }
            trigger.Add(token);
        }

        public static void CancelOnDisable(this ICancelToken token, GameObject gameObject)
        {
            if (token == null)
            {
                return;
            }
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }
            if (!gameObject.TryGetComponent<CancelOnDisableTrigger>(out var trigger))
            {
                trigger = gameObject.AddComponent<CancelOnDisableTrigger>();
            }
            trigger.Add(token);
        }

        public static void CancelAllTokens(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            List<Exception> exceptions = null;
            CancelTrigger<CancelOnDestroyTrigger>(gameObject, ref exceptions);
            CancelTrigger<CancelOnDisableTrigger>(gameObject, ref exceptions);
            if (exceptions != null)
            {
                throw new AggregateException(exceptions);
            }
        }

        private static void CancelTrigger<TTrigger>(GameObject gameObject, ref List<Exception> exceptions) where TTrigger : CancelTokenTrigger
        {
            if (!gameObject.TryGetComponent<TTrigger>(out var trigger))
            {
                return;
            }
            try
            {
                trigger.CancelAll();
            }
            catch (AggregateException exception)
            {
                (exceptions ??= new List<Exception>()).AddRange(exception.InnerExceptions);
            }
        }
    }
}
#endif
