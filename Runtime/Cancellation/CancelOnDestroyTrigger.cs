using UnityEngine;

namespace Aoiro
{
    [DisallowMultipleComponent]
    internal sealed class CancelOnDestroyTrigger : CancelTokenTrigger
    {
        private void OnDestroy() => CancelAll();
    }
}
