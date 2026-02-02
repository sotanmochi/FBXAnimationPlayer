using System.Collections.Generic;
using UnityEngine;

namespace FbxAnimationPlayer
{
    public static class HumanAvatarSkeletonUtility
    {
        /// <summary>
        /// Create a mapping of HumanBodyBones and Transforms.
        /// HumanBodyBonesとTransformのマッピングを作成する。
        /// </summary>
        public static Dictionary<HumanBodyBones, Transform> CreateBoneTransformMap(GameObject skeletonRoot)
        {
            if (skeletonRoot == null)
            {
                return new Dictionary<HumanBodyBones, Transform>();
            }

            var boneTranformMap = new Dictionary<HumanBodyBones, Transform>();
            var mappedTransforms = new HashSet<Transform>();
            var allTransforms = skeletonRoot.GetComponentsInChildren<Transform>();

            foreach (var transform in allTransforms)
            {
                if (mappedTransforms.Contains(transform))
                {
                    continue;
                }

                var normalizedName = transform.name
                                        .ToLowerInvariant()
                                        .Replace(" ", "")
                                        .Replace("_", "")
                                        .Replace("-", "")
                                        .Replace(".", "")
                                        .Replace(":", "");

                foreach (var kvp in BoneNamePatterns)
                {
                    var bone = kvp.Key;
                    if (boneTranformMap.ContainsKey(bone))
                    {
                        continue;
                    }

                    var namePatterns = kvp.Value;
                    foreach (var namePattern in namePatterns)
                    {
                        if (normalizedName == namePattern || normalizedName.Contains(namePattern))
                        {
                            boneTranformMap[bone] = transform;
                            mappedTransforms.Add(transform);
                            break;
                        }
                    }

                    if (mappedTransforms.Contains(transform))
                    {
                        break;
                    }
                }
            }

            return boneTranformMap;
        }

        /// <summary>
        /// Apply T-Pose.
        /// Before executing this process, the bone hierarchy must be constructed,
        /// and the localPosition of each bone must already be set to the correct value.
        /// <br/>
        /// Tポーズを適用する。
        /// この処理を実行する前に、ボーンの階層が構築されていて、各ボーンのlocalPositionは正しい値が設定されている必要がある。
        /// </summary>
        public static void ApplyTPose(Dictionary<HumanBodyBones, Transform> skeletonBones, bool adjustHeightToGround = true)
        {
            if (skeletonBones == null || skeletonBones.Count == 0)
            {
                DebugLogger.LogError($"[HumanAvatarSkeletonUtility] The skeletonBones is null or empty.");
                return;
            }

            ApplyTPoseToHips(skeletonBones);

            var boneChains = BuildDynamicBoneChains(skeletonBones);
            foreach (var (parent, child) in boneChains)
            {
                ApplyTPoseToChain(skeletonBones, parent, child);
            }

            if (adjustHeightToGround)
            {
                AdjustHipsHeightToStandOnGround(skeletonBones);
            }
        }

        /// <summary>
        /// Build dynamic bone chains based on available bones in the skeleton.
        /// This handles optional bones like UpperChest correctly.
        /// <br/>
        /// スケルトンに存在するボーンに基づいて動的にボーンチェーンを構築する。
        /// UpperChestのようなオプションボーンを正しく処理する。
        /// </summary>
        private static List<(HumanBodyBones parent, HumanBodyBones child)> BuildDynamicBoneChains(
            Dictionary<HumanBodyBones, Transform> skeletonBones)
        {
            var chains = new List<(HumanBodyBones parent, HumanBodyBones child)>();

            // Body trunk
            chains.Add((HumanBodyBones.Hips, HumanBodyBones.Spine));
            chains.Add((HumanBodyBones.Spine, HumanBodyBones.Chest));

            // Left leg
            chains.Add((HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg));
            chains.Add((HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg));
            chains.Add((HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot));
            chains.Add((HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes));

            // Right leg
            chains.Add((HumanBodyBones.Hips, HumanBodyBones.RightUpperLeg));
            chains.Add((HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg));
            chains.Add((HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot));
            chains.Add((HumanBodyBones.RightFoot, HumanBodyBones.RightToes));

            // Handle UpperChest as optional bone
            var hasUpperChest = skeletonBones.ContainsKey(HumanBodyBones.UpperChest);
            if (hasUpperChest)
            {
                chains.Add((HumanBodyBones.Chest, HumanBodyBones.UpperChest));
                chains.Add((HumanBodyBones.UpperChest, HumanBodyBones.Neck));
            }
            else
            {
                chains.Add((HumanBodyBones.Chest, HumanBodyBones.Neck));
            }

            // Neck and Head
            chains.Add((HumanBodyBones.Neck, HumanBodyBones.Head));

            // Shoulder parent
            var shoulderParent = hasUpperChest ? HumanBodyBones.UpperChest : HumanBodyBones.Chest;

            // Handle LeftShoulder as optional bone
            if (skeletonBones.ContainsKey(HumanBodyBones.LeftShoulder))
            {
                chains.Add((shoulderParent, HumanBodyBones.LeftShoulder));
                chains.Add((HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm));
            }
            else
            {
                chains.Add((shoulderParent, HumanBodyBones.LeftUpperArm));
            }

            // Left arm
            chains.Add((HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm));
            chains.Add((HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand));

            // Left-hand fingers
            chains.Add((HumanBodyBones.LeftHand, HumanBodyBones.LeftThumbProximal));
            chains.Add((HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftThumbIntermediate));
            chains.Add((HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbDistal));
            chains.Add((HumanBodyBones.LeftHand, HumanBodyBones.LeftIndexProximal));
            chains.Add((HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate));
            chains.Add((HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal));
            chains.Add((HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal));
            chains.Add((HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate));
            chains.Add((HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal));
            chains.Add((HumanBodyBones.LeftHand, HumanBodyBones.LeftRingProximal));
            chains.Add((HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate));
            chains.Add((HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal));
            chains.Add((HumanBodyBones.LeftHand, HumanBodyBones.LeftLittleProximal));
            chains.Add((HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate));
            chains.Add((HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal));

            // Handle RightShoulder as optional bone
            if (skeletonBones.ContainsKey(HumanBodyBones.RightShoulder))
            {
                chains.Add((shoulderParent, HumanBodyBones.RightShoulder));
                chains.Add((HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm));
            }
            else
            {
                chains.Add((shoulderParent, HumanBodyBones.RightUpperArm));
            }

            // Right arm
            chains.Add((HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm));
            chains.Add((HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand));

            // Right-hand fingers
            chains.Add((HumanBodyBones.RightHand, HumanBodyBones.RightThumbProximal));
            chains.Add((HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate));
            chains.Add((HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal));
            chains.Add((HumanBodyBones.RightHand, HumanBodyBones.RightIndexProximal));
            chains.Add((HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate));
            chains.Add((HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal));
            chains.Add((HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal));
            chains.Add((HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate));
            chains.Add((HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal));
            chains.Add((HumanBodyBones.RightHand, HumanBodyBones.RightRingProximal));
            chains.Add((HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate));
            chains.Add((HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal));
            chains.Add((HumanBodyBones.RightHand, HumanBodyBones.RightLittleProximal));
            chains.Add((HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate));
            chains.Add((HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal));

            return chains;
        }

        /// <summary>
        /// Update the rotation of the Hips bone.
        /// It uses the Up direction (Hips -> Spine direction) and Forward direction (calculated from the leg positions).
        /// <br/>
        /// Hipsボーンの回転を更新する。
        /// Up方向（Hips->Spine方向）とForward方向（脚の位置から計算）を使用する。
        /// </summary>
        private static void ApplyTPoseToHips(Dictionary<HumanBodyBones, Transform> skeletonBones)
        {
            if (!skeletonBones.TryGetValue(HumanBodyBones.Hips, out var hips))
            {
                return;
            }

            // Calculate up direction (Hips → Spine)
            var currentUp = Vector3.up;
            if (skeletonBones.TryGetValue(HumanBodyBones.Spine, out var spine))
            {
                var direction = (spine.position - hips.position).normalized;
                if (direction.sqrMagnitude > 0.001f)
                {
                    currentUp = direction;
                }
            }

            // Calculate right direction (LeftUpperLeg → RightUpperLeg)
            var currentRight = Vector3.right;
            if (skeletonBones.TryGetValue(HumanBodyBones.LeftUpperLeg, out var leftLeg) &&
                skeletonBones.TryGetValue(HumanBodyBones.RightUpperLeg, out var rightLeg))
            {
                var direction = (rightLeg.position - leftLeg.position).normalized;
                if (direction.sqrMagnitude > 0.001f)
                {
                    currentRight = direction;
                }
            }

            // Calculate forward direction (Right × Up)
            var currentForward = Vector3.Cross(currentRight, currentUp).normalized;

            // Fallback when the forward direction is nearly zero
            if (currentForward.sqrMagnitude < 0.001f)
            {
                currentForward = Vector3.forward;
            }

            // Calculate correction rotation
            var expectedUp = Vector3.up;
            var expectedForward = Vector3.forward;
            var currentRotation = Quaternion.LookRotation(currentForward, currentUp);
            var expectedRotation = Quaternion.LookRotation(expectedForward, expectedUp);
            var correction = expectedRotation * Quaternion.Inverse(currentRotation);

            // Apply correction to hips rotation
            hips.rotation = correction * hips.rotation;
        }

        private static void ApplyTPoseToChain(Dictionary<HumanBodyBones, Transform> skeletonBones,
            HumanBodyBones parentBone, HumanBodyBones childBone)
        {
            // ----------------------------------------------------------------
            // Skip the chain from Hips to UpperLeg.
            // It is not possible to rotate the hips to point both legs downward,
            // since the upper legs are connected to the Hips.
            // Adjust the leg direction using the chain from UpperLeg to LowerLeg.
            // ----------------------------------------------------------------
            // HipsからUpperLegのチェーンはスキップする。
            // 両脚の付け根はHipsと接続しているため、Hipsを回転させて両脚を下向きにすることはできない。
            // UpperLegからLowerLegのチェーンで脚の向きを調整する。
            // ----------------------------------------------------------------
            if (parentBone == HumanBodyBones.Hips &&
                (childBone == HumanBodyBones.LeftUpperLeg || childBone == HumanBodyBones.RightUpperLeg))
            {
                return;
            }

            // Skip the chain from Chest to Shoulders
            if (parentBone == HumanBodyBones.Chest &&
                (childBone == HumanBodyBones.LeftShoulder || childBone == HumanBodyBones.RightShoulder))
            {
                return;
            }

            // Skip the chain from UpperChest to Shoulders
            if (parentBone == HumanBodyBones.UpperChest && 
                (childBone == HumanBodyBones.LeftShoulder || childBone == HumanBodyBones.RightShoulder))
            {
                return;
            }

            // Skip the chain from Chest to UpperArm
            if (parentBone == HumanBodyBones.Chest &&
                (childBone == HumanBodyBones.LeftUpperArm || childBone == HumanBodyBones.RightUpperArm))
            {
                return;
            }

            // Skip the chain from UpperChest to UpperArm
            if (parentBone == HumanBodyBones.UpperChest &&
                (childBone == HumanBodyBones.LeftUpperArm || childBone == HumanBodyBones.RightUpperArm))
            {
                return;
            }

            if (!skeletonBones.TryGetValue(parentBone, out var parent)) return;
            if (!skeletonBones.TryGetValue(childBone, out var child)) return;
            if (!ExpectedDirections.TryGetValue(childBone, out var expectedDirection)) return;

            var currentDirection = (child.position - parent.position).normalized;
            if (currentDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            // Calculate correction rotation
            var correction = SafeFromToRotation(currentDirection, expectedDirection, GetFallbackAxis(childBone));

            // Apply correction to parent rotation
            parent.rotation = correction * parent.rotation;
        }

        private static Quaternion SafeFromToRotation(Vector3 from, Vector3 to, Vector3 fallbackAxis)
        {
            var dot = Vector3.Dot(from.normalized, to.normalized);

            if (dot > 0.99999f)
            {
                return Quaternion.identity;
            }
            else if (dot < -0.99999f)
            {
                return Quaternion.AngleAxis(180f, fallbackAxis);
            }
            else
            {
                return Quaternion.FromToRotation(from, to);
            }
        }

        private static Vector3 GetFallbackAxis(HumanBodyBones bone)
        {
            var boneName = bone.ToString();

            // Rotate around Z axis for legs
            if (boneName.Contains("Leg") || boneName.Contains("Foot") || boneName.Contains("Toes"))
            {
                return Vector3.forward;
            }

            // Rotate around Y axis for arms, hands, and fingers
            if (boneName.Contains("Arm") || boneName.Contains("Hand") ||
                boneName.Contains("Thumb") || boneName.Contains("Index") ||
                boneName.Contains("Middle") || boneName.Contains("Ring") ||
                boneName.Contains("Little"))
            {
                return Vector3.up;
            }

            // Rotate around Z axis for body trunk, neck, and head
            return Vector3.forward;
        }

        /// <summary>
        /// Adjust Hips height so that the skeleton stands on the ground (Y=0).
        /// The lowest point of Foot or Toes will be at Y=0.
        /// <br/>
        /// スケルトンが地面（Y=0）に立つようにHipsの高さを調整する。
        /// FootまたはToesの最も低い点がY=0になる。
        /// </summary>
        private static void AdjustHipsHeightToStandOnGround(Dictionary<HumanBodyBones, Transform> skeletonBones)
        {
            if (!skeletonBones.TryGetValue(HumanBodyBones.Hips, out var hips))
            {
                return;
            }

            var groundY = GetLowestFootPosition(skeletonBones);
            var hipsHeight = hips.position.y - groundY;
            hips.localPosition = new Vector3(0f, hipsHeight, 0f);
        }

        /// <summary>
        /// Get the lowest Y position of the feet (Toes preferred, Foot as fallback).
        /// <br/>
        /// 足の最も低いY位置を取得する（Toes優先、Footはフォールバック）。
        /// </summary>
        private static float GetLowestFootPosition(Dictionary<HumanBodyBones, Transform> skeletonBones)
        {
            var lowestY = float.MaxValue;

            // Check Toes first (closer to the ground)
            if (skeletonBones.TryGetValue(HumanBodyBones.LeftToes, out var leftToes))
            {
                lowestY = Mathf.Min(lowestY, leftToes.position.y);
            }
            if (skeletonBones.TryGetValue(HumanBodyBones.RightToes, out var rightToes))
            {
                lowestY = Mathf.Min(lowestY, rightToes.position.y);
            }

            // Use Foot if Toes are not found
            if (lowestY == float.MaxValue)
            {
                if (skeletonBones.TryGetValue(HumanBodyBones.LeftFoot, out var leftFoot))
                {
                    lowestY = Mathf.Min(lowestY, leftFoot.position.y);
                }
                if (skeletonBones.TryGetValue(HumanBodyBones.RightFoot, out var rightFoot))
                {
                    lowestY = Mathf.Min(lowestY, rightFoot.position.y);
                }
            }

            return lowestY == float.MaxValue ? 0f : lowestY;
        }

        /// <summary>
        /// Expected directions for each bone chain (in world coordinates).
        /// Defines the direction from parent bone to child bone.
        /// <br/>
        /// 各ボーンチェーンの期待される方向（ワールド座標系）。
        /// 親ボーンから子ボーンへの方向を定義する。
        /// </summary>
        private static readonly Dictionary<HumanBodyBones, Vector3> ExpectedDirections = new()
        {
            // Body trunk: Up direction
            { HumanBodyBones.Spine, Vector3.up },
            { HumanBodyBones.Chest, Vector3.up },
            { HumanBodyBones.UpperChest, Vector3.up },
            { HumanBodyBones.Neck, Vector3.up },
            { HumanBodyBones.Head, Vector3.up },

            // Left arm: Left direction
            { HumanBodyBones.LeftShoulder, Vector3.left },
            { HumanBodyBones.LeftUpperArm, Vector3.left },
            { HumanBodyBones.LeftLowerArm, Vector3.left },
            { HumanBodyBones.LeftHand, Vector3.left },

            // Right arm: Right direction
            { HumanBodyBones.RightShoulder, Vector3.right },
            { HumanBodyBones.RightUpperArm, Vector3.right },
            { HumanBodyBones.RightLowerArm, Vector3.right },
            { HumanBodyBones.RightHand, Vector3.right },

            // Left leg: Down direction
            { HumanBodyBones.LeftUpperLeg, Vector3.down },
            { HumanBodyBones.LeftLowerLeg, Vector3.down },
            { HumanBodyBones.LeftFoot, Vector3.down },
            { HumanBodyBones.LeftToes, new Vector3(0f, -0.4f, 0.9f).normalized },

            // Right leg: Down direction
            { HumanBodyBones.RightUpperLeg, Vector3.down },
            { HumanBodyBones.RightLowerLeg, Vector3.down },
            { HumanBodyBones.RightFoot, Vector3.down },
            { HumanBodyBones.RightToes, new Vector3(0f, -0.4f, 0.9f).normalized },

            // Left-hand fingers
            { HumanBodyBones.LeftThumbProximal, new Vector3(-0.707f, 0f, 0.707f).normalized },
            { HumanBodyBones.LeftThumbIntermediate, new Vector3(-0.707f, 0f, 0.707f).normalized },
            { HumanBodyBones.LeftThumbDistal, new Vector3(-0.707f, 0f, 0.707f).normalized },
            { HumanBodyBones.LeftIndexProximal, Vector3.left },
            { HumanBodyBones.LeftIndexIntermediate, Vector3.left },
            { HumanBodyBones.LeftIndexDistal, Vector3.left },
            { HumanBodyBones.LeftMiddleProximal, Vector3.left },
            { HumanBodyBones.LeftMiddleIntermediate, Vector3.left },
            { HumanBodyBones.LeftMiddleDistal, Vector3.left },
            { HumanBodyBones.LeftRingProximal, Vector3.left },
            { HumanBodyBones.LeftRingIntermediate, Vector3.left },
            { HumanBodyBones.LeftRingDistal, Vector3.left },
            { HumanBodyBones.LeftLittleProximal, Vector3.left },
            { HumanBodyBones.LeftLittleIntermediate, Vector3.left },
            { HumanBodyBones.LeftLittleDistal, Vector3.left },

            // Right-hand fingers
            { HumanBodyBones.RightThumbProximal, new Vector3(0.707f, 0f, 0.707f).normalized },
            { HumanBodyBones.RightThumbIntermediate, new Vector3(0.707f, 0f, 0.707f).normalized },
            { HumanBodyBones.RightThumbDistal, new Vector3(0.707f, 0f, 0.707f).normalized },
            { HumanBodyBones.RightIndexProximal, Vector3.right },
            { HumanBodyBones.RightIndexIntermediate, Vector3.right },
            { HumanBodyBones.RightIndexDistal, Vector3.right },
            { HumanBodyBones.RightMiddleProximal, Vector3.right },
            { HumanBodyBones.RightMiddleIntermediate, Vector3.right },
            { HumanBodyBones.RightMiddleDistal, Vector3.right },
            { HumanBodyBones.RightRingProximal, Vector3.right },
            { HumanBodyBones.RightRingIntermediate, Vector3.right },
            { HumanBodyBones.RightRingDistal, Vector3.right },
            { HumanBodyBones.RightLittleProximal, Vector3.right },
            { HumanBodyBones.RightLittleIntermediate, Vector3.right },
            { HumanBodyBones.RightLittleDistal, Vector3.right },
        };

        private static readonly Dictionary<HumanBodyBones, string[]> BoneNamePatterns = new()
        {
            { HumanBodyBones.Hips, new[] {
                "hips",  "hip", "pelvis",
            }},
            { HumanBodyBones.Spine, new[] {
                "spine", "spine0", "spine1", "spine01",
            }},
            { HumanBodyBones.Chest, new[] {
                "chest", "spine1", "spine01", "spine2", "spine02",
            }},
            { HumanBodyBones.UpperChest, new[] {
                "upperchest", "spine2", "spine02", "spine3", "spine03",
            }},
            { HumanBodyBones.Neck, new[] {
                "neck",
            }},
            { HumanBodyBones.Head, new[] {
                "head",
            }},

            // === Left Arm ===
            { HumanBodyBones.LeftShoulder, new[] {
                "leftshoulder", "shoulderleft",
            }},
            { HumanBodyBones.LeftUpperArm, new[] {
                "leftupperarm", "leftarm", "armleft",
            }},
            { HumanBodyBones.LeftLowerArm, new[] {
                "leftlowerarm", "leftforearm", "forearmleft",
            }},
            { HumanBodyBones.LeftHand, new[] {
                "lefthand", "handleft"
            }},

            // === Right Arm ===
            { HumanBodyBones.RightShoulder, new[] {
                "rightshoulder", "shoulderright",
            }},
            { HumanBodyBones.RightUpperArm, new[] {
                "rightupperarm", "rightarm", "armright",
            }},
            { HumanBodyBones.RightLowerArm, new[] {
                "rightlowerarm", "rightforearm", "forearmright",
            }},
            { HumanBodyBones.RightHand, new[] {
                "righthand", "handright"
            }},

            // === Left Leg ===
            { HumanBodyBones.LeftUpperLeg, new[] {
                "leftupperleg", "leftupleg", "uplegleft",
            }},
            { HumanBodyBones.LeftLowerLeg, new[] {
                "leftlowerleg", "leftleg", "legleft",
            }},
            { HumanBodyBones.LeftFoot, new[] {
                "leftfoot", "footleft",
            }},
            { HumanBodyBones.LeftToes, new[] {
                "lefttoes", "lefttoebase", "toesleft",
            }},

            // === Right Leg ===
            { HumanBodyBones.RightUpperLeg, new[] {
                "rightupperleg", "rightupleg", "uplegright",
            }},
            { HumanBodyBones.RightLowerLeg, new[] {
                "rightlowerleg", "rightleg", "legright",
            }},
            { HumanBodyBones.RightFoot, new[] {
                "rightfoot", "footright",
            }},
            { HumanBodyBones.RightToes, new[] {
                "righttoes", "righttoebase", "toesright",
            }},

            // === Left Hand Fingers ===
            { HumanBodyBones.LeftThumbProximal, new[] {
                "lefthandthumb1", "thumbfinger0left"
            }},
            { HumanBodyBones.LeftThumbIntermediate, new[] {
                "lefthandthumb2", "thumbfinger1left"
            }},
            { HumanBodyBones.LeftThumbDistal, new[] {
                "lefthandthumb3", "thumbfinger2left"
            }},
            { HumanBodyBones.LeftIndexProximal, new[] {
                "lefthandindex1", "indexfinger1left",
            }}, 
            { HumanBodyBones.LeftIndexIntermediate, new[] {
                "lefthandindex2", "indexfinger2left",
            }},
            { HumanBodyBones.LeftIndexDistal, new[] {
                "lefthandindex3", "indexfinger3left",
            }},
            { HumanBodyBones.LeftMiddleProximal, new[] {
                "lefthandmiddle1", "middlefinger1left",
            }},
            { HumanBodyBones.LeftMiddleIntermediate, new[] {
                "lefthandmiddle2", "middlefinger2left",
            }},
            { HumanBodyBones.LeftMiddleDistal, new[] {
                "lefthandmiddle3", "middlefinger3left",
            }},
            { HumanBodyBones.LeftRingProximal, new[] {
                "lefthandring1", "ringfinger1left"
            }},
            { HumanBodyBones.LeftRingIntermediate, new[] {
                "lefthandring2", "ringfinger2left"
            }},
            { HumanBodyBones.LeftRingDistal, new[] {
                "lefthandring3", "ringfinger3left"
            }},
            { HumanBodyBones.LeftLittleProximal, new[] {
                "lefthandpinky1", "pinkyfinger1left",
            }},
            { HumanBodyBones.LeftLittleIntermediate, new[] {
                "lefthandpinky2", "pinkyfinger2left",
            }},
            { HumanBodyBones.LeftLittleDistal, new[] {
                "lefthandpinky3", "pinkyfinger3left",
            }},

            // === Right Hand Fingers ===
            { HumanBodyBones.RightThumbProximal, new[] {
                "righthandthumb1", "thumbfinger0right",
            }},
            { HumanBodyBones.RightThumbIntermediate, new[] {
                "righthandthumb2", "thumbfinger1right",
            }},
            { HumanBodyBones.RightThumbDistal, new[] {
                "righthandthumb3", "thumbfinger2right",
            }},
            { HumanBodyBones.RightIndexProximal, new[] {
                "righthandindex1", "indexfinger1right",
            }},
            { HumanBodyBones.RightIndexIntermediate, new[] {
                "righthandindex2", "indexfinger2right",
            }},
            { HumanBodyBones.RightIndexDistal, new[] {
                "righthandindex3", "indexfinger3right",
            }},
            { HumanBodyBones.RightMiddleProximal, new[] {
                "righthandmiddle1", "middlefinger1right",
            }},
            { HumanBodyBones.RightMiddleIntermediate, new[] {
                "righthandmiddle2", "middlefinger2right",
            }},
            { HumanBodyBones.RightMiddleDistal, new[] {
                "righthandmiddle3", "middlefinger3right",
            }},
            { HumanBodyBones.RightRingProximal, new[] {
                "righthandring1", "ringfinger1right",
            }},
            { HumanBodyBones.RightRingIntermediate, new[] {
                "righthandring2", "ringfinger2right",
            }},
            { HumanBodyBones.RightRingDistal, new[] {
                "righthandring3", "ringfinger3right",
            }},
            { HumanBodyBones.RightLittleProximal, new[] {
                "righthandpinky1", "pinkyfinger1right",
            }},
            { HumanBodyBones.RightLittleIntermediate, new[] {
                "righthandpinky2", "pinkyfinger2right",
            }},
            { HumanBodyBones.RightLittleDistal, new[] {
                "righthandpinky3", "pinkyfinger3right",
            }},
        };
    }
}
