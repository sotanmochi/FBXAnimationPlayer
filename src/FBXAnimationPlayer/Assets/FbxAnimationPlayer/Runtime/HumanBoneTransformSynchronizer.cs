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

        private Quaternion _hipsParentRotationOffset;

        [ReadOnly, NonReorderable, SerializeField]
        private List<Transform> _sourceBones = new(BoneCount);

        [ReadOnly, NonReorderable, SerializeField]
        private List<Transform> _targetBones = new(BoneCount);

        public bool IsEnabled { get; set; } = true;

        void LateUpdate()
        {
            if (!IsEnabled) return;
            SynchronizeBoneTransform();
        }

        public void Setup(Dictionary<HumanBodyBones, Transform> sourceBones,
            Dictionary<HumanBodyBones, Transform> targetBones)
        {
            if (_sourceBones.Count < BoneCount)
            {
                for (var i = _sourceBones.Count; i < BoneCount; i++)
                {
                    _sourceBones.Add(null);
                }
            }

            if (_targetBones.Count < BoneCount)
            {
                for (var i = _targetBones.Count; i < BoneCount; i++)
                {
                    _targetBones.Add(null);
                }
            }

            _sourceHips = sourceBones.GetValueOrDefault(HumanBodyBones.Hips);
            _targetHips = targetBones.GetValueOrDefault(HumanBodyBones.Hips);

            var sourceParentRotation = _sourceHips?.parent != null
                ? _sourceHips.parent.rotation
                : Quaternion.identity;
            var targetParentRotation = _targetHips?.parent != null
                ? _targetHips.parent.rotation
                : Quaternion.identity;
            _hipsParentRotationOffset = Quaternion.Inverse(targetParentRotation) * sourceParentRotation;

            for (var boneId = 0; boneId < BoneCount; boneId++)
            {
                _sourceBones[boneId] = sourceBones.GetValueOrDefault((HumanBodyBones)boneId);
                _targetBones[boneId] = targetBones.GetValueOrDefault((HumanBodyBones)boneId);
            }
        }

        public void Clear()
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
            // Synchronize local position and rotation of hips
            if (_sourceHips != null && _targetHips != null)
            {
                _targetHips.localPosition = _hipsParentRotationOffset * _sourceHips.localPosition;
                _targetHips.localRotation = _hipsParentRotationOffset * _sourceHips.localRotation;
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
