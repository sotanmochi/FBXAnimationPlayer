using System;
using UnityEngine;

namespace FbxAnimationPlayer
{
    public sealed class FbxMotionActor : MonoBehaviour, IDisposable
    {
        private Avatar _avatar;
        private HumanPoseHandler _humanPoseHandler;

        void OnDestroy()
        {
            UnityEngine.Object.Destroy(_avatar);
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
            _avatar = avatar;
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
