using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FbxAnimationPlayer
{
    public static class DebugLogger
    {
        /// <summary>
        /// Logs a message to the Unity Console 
        /// only when DEVELOPMENT_BUILD or UNITY_EDITOR is defined.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(object message)
        {
            UnityEngine.Debug.Log(message);
        }

        /// <summary>
        /// Logs a warning message to the Unity Console 
        /// only when DEVELOPMENT_BUILD or UNITY_EDITOR is defined.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarning(object message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        /// <summary>
        /// Logs an error message to the Unity Console 
        /// only when DEVELOPMENT_BUILD or UNITY_EDITOR is defined.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(object message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}
