using System;
using System.Collections.Generic;
using UnityEngine;

namespace FbxAnimationPlayer
{
    public sealed class FbxAnimationController : MonoBehaviour, IDisposable
    {
        private GameObject _fbxRootObject;
        private List<AnimationClip> _clips;
        private int _currentClipIndex;

        void OnDestroy()
        {
            if (_clips == null) return;

            foreach (var clip in _clips)
            {
                UnityEngine.Object.Destroy(clip);
            }
            _clips.Clear();
        }

        public void Dispose()
        {
            if (this != null && this.gameObject != null)
            {
                UnityEngine.Object.Destroy(this.gameObject);
            }
        }

        public void Setup(GameObject fbxRootObject, List<AnimationClip> clips)
        {
            _fbxRootObject = fbxRootObject;
            _clips = clips;
            _currentClipIndex = 0;
        }
    }
}
