using UnityEngine;

namespace Aoiro
{
    [DisallowMultipleComponent]
    internal sealed class CancelOnDisableTrigger : CancelTokenTrigger
    {
        private void OnDisable() => CancelAll();
    }
}
