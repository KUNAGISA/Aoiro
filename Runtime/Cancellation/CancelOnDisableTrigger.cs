#if UNITY_5_3_OR_NEWER
using UnityEngine;

namespace Aoiro
{
    [DisallowMultipleComponent]
    internal sealed class CancelOnDisableTrigger : CancelTokenTrigger
    {
        private void OnDisable() => CancelAll();
    }
}
#endif
