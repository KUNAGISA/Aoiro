using System;

namespace Aoiro
{
    internal static class AoiroLog
    {
        internal static void Log(object message)
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.Log(message);
#else
            Console.WriteLine(message);
#endif
        }

        internal static void LogWarning(object message)
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogWarning(message);
#else
            Console.Error.WriteLine(message);
#endif
        }

        internal static void LogError(object message)
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogError(message);
#else
            Console.Error.WriteLine(message);
#endif
        }

        internal static void LogException(Exception exception)
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogException(exception);
#else
            Console.Error.WriteLine(exception);
#endif
        }
    }
}
