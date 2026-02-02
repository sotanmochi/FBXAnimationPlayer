using System.Collections.Generic;
using UnityEngine;

namespace FbxAnimationPlayer
{
    public static class HumanAvatarBuilder
    {
        public static Avatar BuildHumanAvatar(GameObject root, Dictionary<HumanBodyBones, Transform> humanAvatarSkeletonBones)
        {
            if (root == null || humanAvatarSkeletonBones == null || humanAvatarSkeletonBones.Count == 0)
            {
                DebugLogger.LogError("Invalid arguments for building human avatar.");
                return null;
            }

            var humanTraitBoneNames = HumanTrait.BoneName;

            var humanBones = new List<HumanBone>();
            foreach (var kvp in humanAvatarSkeletonBones)
            {
                var humanBodyBone = kvp.Key;
                var boneTransform = kvp.Value;

                humanBones.Add(new HumanBone
                {
                    humanName = humanTraitBoneNames[(int)humanBodyBone],
                    boneName = boneTransform.name,
                    limit = new HumanLimit
                    {
                        useDefaultValues = true
                    }
                });
            }

            var skeletonBones = new List<SkeletonBone>();
            skeletonBones.Add(new SkeletonBone()
            {
                name = root.name,
                position = root.transform.localPosition,
                rotation = root.transform.localRotation,
                scale = root.transform.localScale
            });
            foreach (var boneTransform in humanAvatarSkeletonBones.Values)
            {
                skeletonBones.Add(new SkeletonBone()
                {
                    name = boneTransform.name,
                    position = boneTransform.localPosition,
                    rotation = boneTransform.localRotation,
                    scale = boneTransform.localScale
                });
            }

            var humanDescription = new HumanDescription
            {
                human = humanBones.ToArray(),
                skeleton = skeletonBones.ToArray(),
                upperArmTwist = 0.5f,
                lowerArmTwist = 0.5f,
                upperLegTwist = 0.5f,
                lowerLegTwist = 0.5f,
                armStretch = 0.05f,
                legStretch = 0.05f,
                feetSpacing = 0f,
                hasTranslationDoF = false
            };

            var avatar = AvatarBuilder.BuildHumanAvatar(root, humanDescription);
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                DebugLogger.LogError("Failed to build a human avatar.");
                return null;
            }

            return avatar;
        }
    }
}
