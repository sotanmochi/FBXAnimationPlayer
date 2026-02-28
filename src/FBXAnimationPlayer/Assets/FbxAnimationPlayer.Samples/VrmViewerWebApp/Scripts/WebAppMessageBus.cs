using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FbxAnimationPlayer.Samples
{
    public sealed class WebAppMessageBus : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void DispatchEvent(string type, string payloadJson);
#endif

        public event Action<Message> MessageReceived;

        /// <summary>
        /// Called from JavaScript via SendMessage('WebAppMessageBus', 'OnMessage', json).
        /// </summary>
        public void OnMessage(string json)
        {
            var msg = JsonUtility.FromJson<Message>(json);
            if (string.IsNullOrEmpty(msg.type))
            {
                Debug.Log($"[{nameof(WebAppMessageBus)}] Received message with empty type.");
                return;
            }
            MessageReceived?.Invoke(msg);
        }

        /// <summary>
        /// Emit an event from C# to JavaScript.
        /// </summary>
        public void Emit(string type, string payloadJson = "{}")
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DispatchEvent(type, payloadJson);
#else
            Debug.Log($"[{nameof(WebAppMessageBus)}] Emit | type={type} payload={payloadJson}");
#endif
        }
    }
}
