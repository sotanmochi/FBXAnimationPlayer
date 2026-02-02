using UnityEngine;

namespace FbxAnimationPlayer
{
    public sealed class FbxMotionActor : MonoBehaviour
    {
        private HumanPoseHandler _humanPoseHandler;

        public void SetHumanAvatar(Avatar avatar, Transform avatarRoot)
        {
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
