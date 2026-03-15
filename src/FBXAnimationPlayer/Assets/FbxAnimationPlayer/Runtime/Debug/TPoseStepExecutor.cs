using System.Collections.Generic;
using UnityEngine;

namespace FbxAnimationPlayer
{
    public class TPoseStepExecutor
    {
        private Dictionary<HumanBodyBones, Transform> _skeletonBones;
        private readonly List<TPoseStepSnapshot> _snapshots = new();
        private int _currentStepIndex = -1;
        private List<(HumanBodyBones parent, HumanBodyBones child)> _boneChains;

        // Step layout:
        //   0        : Initial state (identity rotations after clone)
        //   1        : ApplyTPoseToHips
        //   2..N+1   : ApplyTPoseToChain for each bone chain (N chains)
        //   N+2      : AdjustHipsHeightToStandOnGround
        public int TotalSteps { get; private set; }
        public int CurrentStep => _currentStepIndex;
        public IReadOnlyList<TPoseStepSnapshot> Snapshots => _snapshots;
        public IReadOnlyList<(HumanBodyBones parent, HumanBodyBones child)> BoneChains => _boneChains;

        public void Initialize(Dictionary<HumanBodyBones, Transform> skeletonBones)
        {
            _skeletonBones = skeletonBones;
            _boneChains = HumanAvatarSkeletonUtility.BuildDynamicBoneChains(skeletonBones);
            // steps: initial(1) + hips(1) + chains(N) + height(1)
            TotalSteps = 1 + 1 + _boneChains.Count + 1;
            _currentStepIndex = -1;
            _snapshots.Clear();
        }

        public TPoseStepSnapshot ExecuteNextStep()
        {
            var nextIndex = _currentStepIndex + 1;
            if (nextIndex >= TotalSteps) return null;

            _currentStepIndex = nextIndex;

            TPoseStepSnapshot snapshot;
            if (nextIndex == 0)
            {
                snapshot = CaptureInitialState();
            }
            else if (nextIndex == 1)
            {
                snapshot = ExecuteHipsStep();
            }
            else if (nextIndex < TotalSteps - 1)
            {
                var chainIndex = nextIndex - 2;
                snapshot = ExecuteChainStep(chainIndex);
            }
            else
            {
                snapshot = ExecuteHeightAdjustmentStep();
            }

            snapshot.StepIndex = nextIndex;
            _snapshots.Add(snapshot);
            return snapshot;
        }

        public void ExecuteToStep(int targetStep)
        {
            if (targetStep < 0) targetStep = 0;
            if (targetStep >= TotalSteps) targetStep = TotalSteps - 1;

            if (targetStep <= _currentStepIndex)
            {
                // Need to go backwards: reset and re-execute
                ResetSkeleton();
                _currentStepIndex = -1;
                _snapshots.Clear();
            }

            while (_currentStepIndex < targetStep)
            {
                ExecuteNextStep();
            }
        }

        public void GoToStep(int targetStep)
        {
            if (targetStep < 0 || targetStep >= TotalSteps) return;
            if (targetStep < _snapshots.Count)
            {
                // We already have a snapshot for this step, just restore it
                TPoseStepSnapshot.RestoreBoneStates(_skeletonBones, _snapshots[targetStep].BoneStates);
                _currentStepIndex = targetStep;
            }
            else
            {
                ExecuteToStep(targetStep);
            }
        }

        public void Reset()
        {
            ResetSkeleton();
            _currentStepIndex = -1;
            _snapshots.Clear();
        }

        private void ResetSkeleton()
        {
            foreach (var kvp in _skeletonBones)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.localRotation = Quaternion.identity;
                }
            }
            // Restore initial localPositions from step 0 if available
            if (_snapshots.Count > 0)
            {
                var initialSnapshot = _snapshots[0];
                foreach (var kvp in initialSnapshot.BoneStates)
                {
                    if (_skeletonBones.TryGetValue(kvp.Key, out var t) && t != null)
                    {
                        t.localPosition = kvp.Value.LocalPosition;
                    }
                }
            }
        }

        private TPoseStepSnapshot CaptureInitialState()
        {
            return new TPoseStepSnapshot
            {
                StepName = "Initial State",
                Description = "クローンスケルトンの初期状態（全ボーン identity rotation）",
                WasSkipped = false,
                DebugInfo = new StepDebugInfo(),
                BoneStates = TPoseStepSnapshot.CaptureBoneStates(_skeletonBones),
            };
        }

        private TPoseStepSnapshot ExecuteHipsStep()
        {
            // Capture debug info by computing the same values as ApplyTPoseToHips
            var debugInfo = new StepDebugInfo();

            if (_skeletonBones.TryGetValue(HumanBodyBones.Hips, out var hips))
            {
                var currentUp = Vector3.up;
                if (_skeletonBones.TryGetValue(HumanBodyBones.Spine, out var spine))
                {
                    var direction = (spine.position - hips.position).normalized;
                    if (direction.sqrMagnitude > 0.001f)
                        currentUp = direction;
                }

                var currentRight = Vector3.right;
                if (_skeletonBones.TryGetValue(HumanBodyBones.LeftUpperLeg, out var leftLeg) &&
                    _skeletonBones.TryGetValue(HumanBodyBones.RightUpperLeg, out var rightLeg))
                {
                    var direction = (rightLeg.position - leftLeg.position).normalized;
                    if (direction.sqrMagnitude > 0.001f)
                        currentRight = direction;
                }

                var currentForward = Vector3.Cross(currentRight, currentUp).normalized;
                if (currentForward.sqrMagnitude < 0.001f)
                    currentForward = Vector3.forward;

                var currentRotation = Quaternion.LookRotation(currentForward, currentUp);
                var expectedRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                var correction = expectedRotation * Quaternion.Inverse(currentRotation);

                debugInfo.CurrentUp = currentUp;
                debugInfo.CurrentRight = currentRight;
                debugInfo.CurrentForward = currentForward;
                debugInfo.CorrectionRotation = correction;
            }

            // Execute the actual step
            HumanAvatarSkeletonUtility.ApplyTPoseToHips(_skeletonBones);

            return new TPoseStepSnapshot
            {
                StepName = "ApplyTPoseToHips",
                Description = "Hipsの回転補正（Spine方向 + 脚位置から計算）",
                WasSkipped = false,
                DebugInfo = debugInfo,
                BoneStates = TPoseStepSnapshot.CaptureBoneStates(_skeletonBones),
            };
        }

        private TPoseStepSnapshot ExecuteChainStep(int chainIndex)
        {
            var (parentBone, childBone) = _boneChains[chainIndex];
            var debugInfo = new StepDebugInfo
            {
                ParentBone = parentBone,
                ChildBone = childBone,
            };

            // Check if this chain would be skipped
            bool isSkipped = IsChainSkipped(parentBone, childBone, out var skipReason);

            if (!isSkipped)
            {
                // Capture direction info before applying
                if (_skeletonBones.TryGetValue(parentBone, out var parent) &&
                    _skeletonBones.TryGetValue(childBone, out var child) &&
                    HumanAvatarSkeletonUtility.ExpectedDirections.TryGetValue(childBone, out var expectedDir))
                {
                    var currentDir = (child.position - parent.position).normalized;
                    debugInfo.CurrentDirection = currentDir;
                    debugInfo.ExpectedDirection = expectedDir;
                    debugInfo.DotProduct = Vector3.Dot(currentDir, expectedDir);
                    debugInfo.CorrectionRotation = HumanAvatarSkeletonUtility.SafeFromToRotation(
                        currentDir, expectedDir,
                        HumanAvatarSkeletonUtility.GetFallbackAxis(childBone));
                }
            }

            // Execute the actual step (it handles skip logic internally)
            HumanAvatarSkeletonUtility.ApplyTPoseToChain(_skeletonBones, parentBone, childBone);

            return new TPoseStepSnapshot
            {
                StepName = $"Chain: {parentBone} -> {childBone}",
                Description = isSkipped
                    ? $"スキップ: {skipReason}"
                    : $"{parentBone} から {childBone} への方向補正",
                WasSkipped = isSkipped,
                SkipReason = skipReason,
                DebugInfo = debugInfo,
                BoneStates = TPoseStepSnapshot.CaptureBoneStates(_skeletonBones),
            };
        }

        private TPoseStepSnapshot ExecuteHeightAdjustmentStep()
        {
            var debugInfo = new StepDebugInfo();

            debugInfo.GroundY = HumanAvatarSkeletonUtility.GetLowestFootPosition(_skeletonBones);
            if (_skeletonBones.TryGetValue(HumanBodyBones.Hips, out var hips))
            {
                debugInfo.AdjustedHipsHeight = hips.position.y - debugInfo.GroundY;
            }

            HumanAvatarSkeletonUtility.AdjustHipsHeightToStandOnGround(_skeletonBones);

            return new TPoseStepSnapshot
            {
                StepName = "AdjustHipsHeight",
                Description = "スケルトンが地面（Y=0）に立つようにHipsの高さを調整",
                WasSkipped = false,
                DebugInfo = debugInfo,
                BoneStates = TPoseStepSnapshot.CaptureBoneStates(_skeletonBones),
            };
        }

        private static bool IsChainSkipped(HumanBodyBones parent, HumanBodyBones child, out string reason)
        {
            if (parent == HumanBodyBones.Hips &&
                (child == HumanBodyBones.LeftUpperLeg || child == HumanBodyBones.RightUpperLeg))
            {
                reason = "Hips→UpperLeg: 両脚の付け根はHipsに接続されているためスキップ";
                return true;
            }

            if ((parent == HumanBodyBones.Chest || parent == HumanBodyBones.UpperChest) &&
                (child == HumanBodyBones.LeftShoulder || child == HumanBodyBones.RightShoulder))
            {
                reason = "Chest/UpperChest→Shoulder: 肩幅への影響を避けるためスキップ";
                return true;
            }

            if ((parent == HumanBodyBones.Chest || parent == HumanBodyBones.UpperChest) &&
                (child == HumanBodyBones.LeftUpperArm || child == HumanBodyBones.RightUpperArm))
            {
                reason = "Chest/UpperChest→UpperArm: 腕の回転問題を避けるためスキップ";
                return true;
            }

            reason = null;
            return false;
        }
    }
}
