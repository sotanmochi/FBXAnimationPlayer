using System;
using UnityEngine;

namespace FbxAnimationPlayer
{
    public sealed class FbxMotionActor : MonoBehaviour, IDisposable
    {
        private HumanPoseHandler _humanPoseHandler;

        void OnDestroy()
        {
            _humanPoseHandler?.Dispose();
            _humanPoseHandler = null;
        }

        public void Dispose()
        {
            if (this != null && this.gameObject != null)
            {
                UnityEngine.Object.Destroy(this.gameObject);
            }
        }

        public void SetHumanAvatar(Avatar avatar, Transform avatarRoot)
        {
            _humanPoseHandler?.Dispose();
            _humanPoseHandler = new HumanPoseHandler(avatar, avatarRoot);
        }

        public bool TryGetHumanPose(ref HumanPose humanPose)
        {
            if (_humanPoseHandler == null)
            {
                humanPose = default;
                return false;
            }

            _humanPoseHandler.GetHumanPose(ref humanPose);
            return true;
        }
    }
}
