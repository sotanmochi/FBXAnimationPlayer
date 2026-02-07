using System;
using System.Collections.Generic;
using UnityEngine;

namespace FbxAnimationPlayer
{
    public sealed class FbxAnimationController : MonoBehaviour
    {
        private GameObject _fbxRootObject;
        private List<AnimationClip> _clips;
        private int _currentClipIndex;

        void OnDestroy()
        {
            foreach (var clip in _clips)
            {
                Destroy(clip);
            }
            _clips.Clear();
        }

        public void Setup(GameObject fbxRootObject, List<AnimationClip> clips)
        {
            _fbxRootObject = fbxRootObject;
            _clips = clips;
            _currentClipIndex = 0;
        }
    }
}
