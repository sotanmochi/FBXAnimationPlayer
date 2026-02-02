using System.Collections.Generic;
using UnityEngine;

namespace FbxAnimationPlayer
{
    public sealed class HumanBoneTransformSynchronizer : MonoBehaviour
    {
        private const int BoneCount = (int)HumanBodyBones.LastBone;

        [ReadOnly, SerializeField]
        private Transform _sourceHips;

        [ReadOnly, SerializeField]
        private Transform _targetHips;

        [ReadOnly, NonReorderable, SerializeField]
        private List<Transform> _sourceBones = new(BoneCount);

        [ReadOnly, NonReorderable, SerializeField]
        private List<Transform> _targetBones = new(BoneCount);

        public bool IsEnabled { get; set; } = true;
        public List<Transform> SourceBones => _sourceBones;
        public List<Transform> TargetBones => _targetBones;

        void Awake()
        {
            for (var i = 0; i < BoneCount; i++)
            {
                _sourceBones.Add(null);
                _targetBones.Add(null);
            }
        }

        void LateUpdate()
        {
            if (!IsEnabled) return;
            SynchronizeBoneTransform();
        }

        public void Setup(Dictionary<HumanBodyBones, Transform> sourceBones,
            Dictionary<HumanBodyBones, Transform> targetBones)
        {
            _sourceHips = sourceBones.GetValueOrDefault(HumanBodyBones.Hips);
            _targetHips = targetBones.GetValueOrDefault(HumanBodyBones.Hips);

            for (var boneId = 0; boneId < BoneCount; boneId++)
            {
                _sourceBones[boneId] = sourceBones.GetValueOrDefault((HumanBodyBones)boneId);
                _targetBones[boneId] = targetBones.GetValueOrDefault((HumanBodyBones)boneId);
            }
        }

        public void Reset()
        {
            _sourceHips = null;
            _targetHips = null;

            for (var i = 0; i < BoneCount; i++)
            {
                _sourceBones[i] = null;
                _targetBones[i] = null;
            }
        }

        private void SynchronizeBoneTransform()
        {
            // Synchronize world position and rotation of hips
            if (_sourceHips != null && _targetHips != null)
            {
                _targetHips.position = _sourceHips.position;
                _targetHips.rotation = _sourceHips.rotation;
            }

            // Synchronize local rotation of other bones
            for (var boneId = 0; boneId < BoneCount; boneId++)
            {
                var sourceBone = _sourceBones[boneId];
                var targetBone = _targetBones[boneId];

                if (sourceBone == _sourceHips)
                {
                    continue;
                }

                if (sourceBone != null && targetBone != null)
                {
                    targetBone.localRotation = sourceBone.localRotation;
                }
            }
        }
    }
}
