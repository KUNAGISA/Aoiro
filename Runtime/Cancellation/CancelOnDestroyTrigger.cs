#if UNITY_5_3_OR_NEWER
using UnityEngine;

namespace Aoiro
{
    [DisallowMultipleComponent]
    internal sealed class CancelOnDestroyTrigger : CancelTokenTrigger
    {
        private void OnDestroy() => CancelAll();
    }
}
#endif
