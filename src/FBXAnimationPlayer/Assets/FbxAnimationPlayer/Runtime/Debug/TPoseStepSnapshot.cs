using System.Collections.Generic;
using UnityEngine;

namespace FbxAnimationPlayer
{
    public struct BoneTransformData
    {
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 WorldPosition;
        public Quaternion WorldRotation;
    }

    public class StepDebugInfo
    {
        // ApplyTPoseToHips
        public Vector3? CurrentUp;
        public Vector3? CurrentRight;
        public Vector3? CurrentForward;
        public Quaternion? CorrectionRotation;

        // ApplyTPoseToChain
        public HumanBodyBones? ParentBone;
        public HumanBodyBones? ChildBone;
        public Vector3? CurrentDirection;
        public Vector3? ExpectedDirection;
        public float? DotProduct;

        // AdjustHipsHeightToStandOnGround
        public float? GroundY;
        public float? AdjustedHipsHeight;
    }

    public class TPoseStepSnapshot
    {
        public string StepName;
        public string Description;
        public int StepIndex;
        public bool WasSkipped;
        public string SkipReason;
        public Dictionary<HumanBodyBones, BoneTransformData> BoneStates;
        public StepDebugInfo DebugInfo;

        public static Dictionary<HumanBodyBones, BoneTransformData> CaptureBoneStates(
            Dictionary<HumanBodyBones, Transform> skeletonBones)
        {
            var states = new Dictionary<HumanBodyBones, BoneTransformData>();
            foreach (var kvp in skeletonBones)
            {
                var t = kvp.Value;
                if (t == null) continue;
                states[kvp.Key] = new BoneTransformData
                {
                    LocalPosition = t.localPosition,
                    LocalRotation = t.localRotation,
                    WorldPosition = t.position,
                    WorldRotation = t.rotation,
                };
            }
            return states;
        }

        public static void RestoreBoneStates(
            Dictionary<HumanBodyBones, Transform> skeletonBones,
            Dictionary<HumanBodyBones, BoneTransformData> states)
        {
            foreach (var kvp in states)
            {
                if (skeletonBones.TryGetValue(kvp.Key, out var t) && t != null)
                {
                    t.localPosition = kvp.Value.LocalPosition;
                    t.localRotation = kvp.Value.LocalRotation;
                }
            }
        }
    }
}
