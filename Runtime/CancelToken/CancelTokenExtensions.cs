using System.Collections.Generic;

namespace Aoiro
{
    public static class CancelTokenExtensions
    {
        internal class CancelTokenTrigger : UnityEngine.MonoBehaviour
        {
            private readonly HashSet<ICancelToken> _cancelTokens = new HashSet<ICancelToken>();

            private void Awake() => hideFlags = UnityEngine.HideFlags.HideAndDontSave;

            public void Add(ICancelToken token)
            {
                if (token != null)
                {
                    _cancelTokens.Add(token);
                }
            }

            public void CancelAll()
            {
                foreach (var cancelToken in _cancelTokens)
                {
                    cancelToken.Cancel();
                }
                _cancelTokens.Clear();
            }
        }

        [UnityEngine.DisallowMultipleComponent]
        internal class CancelOnDestroyTrigger : CancelTokenTrigger
        {
            private void OnDestroy() => CancelAll();
        }

        [UnityEngine.DisallowMultipleComponent]
        internal class CancelOnDisableTrigger : CancelTokenTrigger
        {
            private void OnDisable() => CancelAll();
        }
        
        public static void CancelWhenGameObjectDestroyed(this ICancelToken self, UnityEngine.GameObject gameObject)
        {
            if (self == null)
            {
                return;
            }

            if (!gameObject.TryGetComponent<CancelOnDestroyTrigger>(out var trigger))
            {
                trigger = gameObject.AddComponent<CancelOnDestroyTrigger>();
            }
            trigger.Add(self);
        }

        public static void CancelWhenGameObjectDisabled(this ICancelToken self, UnityEngine.GameObject gameObject)
        {
            if (self == null)
            {
                return;
            }

            if (!gameObject.TryGetComponent<CancelOnDisableTrigger>(out var trigger))
            {
                trigger = gameObject.AddComponent<CancelOnDisableTrigger>();
            }
            trigger.Add(self);
        }
        
        public static void CancelAllCancelToken(this UnityEngine.GameObject self)
        {
            if (self.TryGetComponent<CancelOnDestroyTrigger>(out var cancelOnDestroyTrigger))
            {
                cancelOnDestroyTrigger.CancelAll();
            }
            if (self.TryGetComponent<CancelOnDisableTrigger>(out var cancelOnDisableTrigger))
            {
                cancelOnDisableTrigger.CancelAll();
            }
        }
    }
}
