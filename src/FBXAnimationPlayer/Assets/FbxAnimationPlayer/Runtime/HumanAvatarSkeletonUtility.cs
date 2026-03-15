using System.Collections.Generic;
using UnityEngine;

namespace FbxAnimationPlayer
{
    public static class HumanAvatarSkeletonUtility
    {
        /// <summary>
        /// Create a mapping of HumanBodyBones and Transforms using built-in name patterns.
        /// 組み込みの名前パターンを使用して、HumanBodyBonesとTransformのマッピングを作成する。
        /// </summary>
        public static Dictionary<HumanBodyBones, Transform> CreateBoneTransformMap(GameObject skeletonRoot)
        {
            return CreateBoneTransformMap(skeletonRoot, null, null);
        }

        /// <summary>
        /// Create a mapping of HumanBodyBones and Transforms using custom prefixes and bone name patterns.
        /// カスタムのプレフィックスとボーン名パターンを使用して、HumanBodyBonesとTransformのマッピングを作成する。
        /// </summary>
        /// <param name="skeletonRoot">The root GameObject of the skeleton.</param>
        /// <param name="customPrefixesToStrip">Prefixes to strip from bone names. If null, built-in defaults are used.</param>
        /// <param name="customBoneNamePatterns">Bone name patterns for matching. If null, built-in defaults are used.</param>
        public static Dictionary<HumanBodyBones, Transform> CreateBoneTransformMap(
            GameObject skeletonRoot,
            string[] customPrefixesToStrip,
            List<(HumanBodyBones bone, string[] namePatterns)> customBoneNamePatterns)
        {
            if (skeletonRoot == null)
            {
                return new Dictionary<HumanBodyBones, Transform>();
            }

            var prefixesToStrip = customPrefixesToStrip ?? DefaultPrefixesToStrip;
            var boneNamePatterns = customBoneNamePatterns ?? DefaultBoneNamePatterns;

            var boneTransformMap = new Dictionary<HumanBodyBones, Transform>();
            var mappedTransforms = new HashSet<Transform>();
            var allTransforms = skeletonRoot.GetComponentsInChildren<Transform>();

            foreach (var transform in allTransforms)
            {
                if (mappedTransforms.Contains(transform))
                {
                    continue;
                }

                // Remove prefixes and convert to lowercase
                // ボーン名を小文字に変換してプレフィックスを除去する
                var transformName = transform.name.ToLowerInvariant();
                foreach (var prefix in prefixesToStrip)
                {
                    if (transformName.StartsWith(prefix))
                    {
                        transformName = transformName.Substring(prefix.Length);
                        break;
                    }
                }

                foreach (var (bone, namePatterns) in boneNamePatterns)
                {
                    if (boneTransformMap.ContainsKey(bone))
                    {
                        continue;
                    }

                    foreach (var namePattern in namePatterns)
                    {
                        if (transformName.StartsWith(namePattern))
                        {
                            boneTransformMap[bone] = transform;
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

            return boneTransformMap;
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
        internal static List<(HumanBodyBones parent, HumanBodyBones child)> BuildDynamicBoneChains(
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
        internal static void ApplyTPoseToHips(Dictionary<HumanBodyBones, Transform> skeletonBones)
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

        internal static void ApplyTPoseToChain(Dictionary<HumanBodyBones, Transform> skeletonBones,
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

        internal static Quaternion SafeFromToRotation(Vector3 from, Vector3 to, Vector3 fallbackAxis)
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

        internal static Vector3 GetFallbackAxis(HumanBodyBones bone)
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
        internal static void AdjustHipsHeightToStandOnGround(Dictionary<HumanBodyBones, Transform> skeletonBones)
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
        internal static float GetLowestFootPosition(Dictionary<HumanBodyBones, Transform> skeletonBones)
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
        internal static readonly Dictionary<HumanBodyBones, Vector3> ExpectedDirections = new()
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

        public static readonly string[] DefaultPrefixesToStrip = new[]
        {
            "mixamorig:",   // Mixamo (e.g., "mixamorig:LeftArm")
        };

        public static readonly List<(HumanBodyBones bone, string[] namePatterns)> DefaultBoneNamePatterns = new()
        {
            // === Body ===
            (HumanBodyBones.Hips, new[] {
                "hips", "hip", "pelvis",
            }),
            (HumanBodyBones.Spine, new[] {
                "spine", "spine0", "spine1", "spine01", "spine_01",
            }),
            (HumanBodyBones.Chest, new[] {
                "chest",
                "spine1", "spine01", "spine_01",
                "spine2", "spine02", "spine_02",
            }),
            (HumanBodyBones.UpperChest, new[] {
                "upperchest", "upper_chest",
                "spine2", "spine02", "spine_02",
                "spine3", "spine03", "spine_03",
            }),
            (HumanBodyBones.Neck, new[] {
                "neck",
            }),
            (HumanBodyBones.Head, new[] {
                "head",
            }),

            // === Left Arm ===
            (HumanBodyBones.LeftShoulder, new[] {
                "leftshoulder", "left_shoulder",
                "shoulder_left", "shoulder_l", "shoulder.l",
            }),
            (HumanBodyBones.LeftUpperArm, new[] {
                "leftupperarm", "left_upper_arm", "left_upperarm",
                "leftarm",
                "arm_left", "arm_l", "arm.l",
                "upperarm_l", "upperarm.l",
            }),
            (HumanBodyBones.LeftLowerArm, new[] {
                "leftlowerarm", "left_lower_arm", "left_lowerarm",
                "leftforearm",
                "forearm_left", "forearm_l", "forearm.l",
                "lowerarm_l", "lowerarm.l",
            }),
            (HumanBodyBones.LeftHand, new[] {
                "lefthand", "left_hand",
                "hand_left", "hand_l", "hand.l",
            }),

            // === Right Arm ===
            (HumanBodyBones.RightShoulder, new[] {
                "rightshoulder", "right_shoulder",
                "shoulder_right", "shoulder_r", "shoulder.r",
            }),
            (HumanBodyBones.RightUpperArm, new[] {
                "rightupperarm", "right_upper_arm", "right_upperarm",
                "rightarm",
                "arm_right", "arm_r", "arm.r",
                "upperarm_r", "upperarm.r",
            }),
            (HumanBodyBones.RightLowerArm, new[] {
                "rightlowerarm", "right_lower_arm", "right_lowerarm",
                "rightforearm",
                "forearm_right", "forearm_r", "forearm.r",
                "lowerarm_r", "lowerarm.r",
            }),
            (HumanBodyBones.RightHand, new[] {
                "righthand", "right_hand",
                "hand_right", "hand_r", "hand.r",
            }),

            // === Left Leg ===
            (HumanBodyBones.LeftUpperLeg, new[] {
                "leftupperleg", "left_upper_leg", "left_upperleg",
                "leftupleg",
                "upleg_left", "upleg_l", "upleg.l",
                "upperleg_l", "upperleg.l",
            }),
            (HumanBodyBones.LeftLowerLeg, new[] {
                "leftlowerleg", "left_lower_leg", "left_lowerleg",
                "leftleg",
                "leg_left", "leg_l", "leg.l",
                "lowerleg_l", "lowerleg.l",
            }),
            (HumanBodyBones.LeftFoot, new[] {
                "leftfoot", "left_foot",
                "foot_left", "foot_l", "foot.l",
            }),
            (HumanBodyBones.LeftToes, new[] {
                "lefttoebase", "left_toebase", "left_toe_base",
                "lefttoes", "left_toes",
                "toebase_left", "toes_left",
                "toebase_l", "toes_l", "toes.l",
            }),

            // === Right Leg ===
            (HumanBodyBones.RightUpperLeg, new[] {
                "rightupperleg", "right_upper_leg", "right_upperleg",
                "rightupleg",
                "upleg_right", "upleg_r", "upleg.r",
                "upperleg_r", "upperleg.r",
            }),
            (HumanBodyBones.RightLowerLeg, new[] {
                "rightlowerleg", "right_lower_leg", "right_lowerleg",
                "rightleg",
                "leg_right", "leg_r", "leg.r",
                "lowerleg_r", "lowerleg.r",
            }),
            (HumanBodyBones.RightFoot, new[] {
                "rightfoot", "right_foot",
                "foot_right", "foot_r", "foot.r",
            }),
            (HumanBodyBones.RightToes, new[] {
                "righttoebase", "right_toebase", "right_toe_base",
                "righttoes", "right_toes",
                "toebase_right", "toes_right",
                "toebase_r", "toes_r", "toes.r",
            }),

            // === Left Hand Fingers ===
            (HumanBodyBones.LeftThumbProximal, new[] {
                "lefthandthumb1",
                "thumbfinger0_left",
                "thumb1_l", "thumb1.l",
            }),
            (HumanBodyBones.LeftThumbIntermediate, new[] {
                "lefthandthumb2",
                "thumbfinger1_left",
                "thumb2_l", "thumb2.l",
                "fingerth1_l",
            }),
            (HumanBodyBones.LeftThumbDistal, new[] {
                "lefthandthumb3",
                "thumbfinger2_left",
                "thumb3_l", "thumb3.l",
            }),
            (HumanBodyBones.LeftIndexProximal, new[] {
                "lefthandindex1",
                "indexfinger1_left",
                "index1_l", "index1.l",
            }),
            (HumanBodyBones.LeftIndexIntermediate, new[] {
                "lefthandindex2",
                "indexfinger2_left",
                "index2_l", "index2.l",
            }),
            (HumanBodyBones.LeftIndexDistal, new[] {
                "lefthandindex3",
                "indexfinger3_left",
                "index3_l", "index3.l",
            }),
            (HumanBodyBones.LeftMiddleProximal, new[] {
                "lefthandmiddle1",
                "middlefinger1_left",
                "middle1_l", "middle1.l",
            }),
            (HumanBodyBones.LeftMiddleIntermediate, new[] {
                "lefthandmiddle2",
                "middlefinger2_left",
                "middle2_l", "middle2.l",
            }),
            (HumanBodyBones.LeftMiddleDistal, new[] {
                "lefthandmiddle3",
                "middlefinger3_left",
                "middle3_l", "middle3.l",
            }),
            (HumanBodyBones.LeftRingProximal, new[] {
                "lefthandring1",
                "ringfinger1_left",
                "ring1_l", "ring1.l",
            }),
            (HumanBodyBones.LeftRingIntermediate, new[] {
                "lefthandring2",
                "ringfinger2_left",
                "ring2_l", "ring2.l",
            }),
            (HumanBodyBones.LeftRingDistal, new[] {
                "lefthandring3",
                "ringfinger3_left",
                "ring3_l", "ring3.l",
            }),
            (HumanBodyBones.LeftLittleProximal, new[] {
                "lefthandpinky1",
                "pinkyfinger1_left",
                "pinky1_l", "pinky1.l",
            }),
            (HumanBodyBones.LeftLittleIntermediate, new[] {
                "lefthandpinky2",
                "pinkyfinger2_left",
                "pinky2_l", "pinky2.l",
            }),
            (HumanBodyBones.LeftLittleDistal, new[] {
                "lefthandpinky3",
                "pinkyfinger3_left",
                "pinky3_l", "pinky3.l",
            }),

            // === Right Hand Fingers ===
            (HumanBodyBones.RightThumbProximal, new[] {
                "righthandthumb1",
                "thumbfinger0_right",
                "thumb1_r", "thumb1.r",
            }),
            (HumanBodyBones.RightThumbIntermediate, new[] {
                "righthandthumb2",
                "thumbfinger1_right",
                "thumb2_r", "thumb2.r",
            }),
            (HumanBodyBones.RightThumbDistal, new[] {
                "righthandthumb3",
                "thumbfinger2_right",
                "thumb3_r", "thumb3.r",
            }),
            (HumanBodyBones.RightIndexProximal, new[] {
                "righthandindex1",
                "indexfinger1_right",
                "index1_r", "index1.r",
            }),
            (HumanBodyBones.RightIndexIntermediate, new[] {
                "righthandindex2",
                "indexfinger2_right",
                "index2_r", "index2.r",
            }),
            (HumanBodyBones.RightIndexDistal, new[] {
                "righthandindex3",
                "indexfinger3_right",
                "index3_r", "index3.r",
            }),
            (HumanBodyBones.RightMiddleProximal, new[] {
                "righthandmiddle1",
                "middlefinger1_right",
                "middle1_r", "middle1.r",
            }),
            (HumanBodyBones.RightMiddleIntermediate, new[] {
                "righthandmiddle2",
                "middlefinger2_right",
                "middle2_r", "middle2.r",
            }),
            (HumanBodyBones.RightMiddleDistal, new[] {
                "righthandmiddle3",
                "middlefinger3_right",
                "middle3_r", "middle3.r",
            }),
            (HumanBodyBones.RightRingProximal, new[] {
                "righthandring1",
                "ringfinger1_right",
                "ring1_r", "ring1.r",
            }),
            (HumanBodyBones.RightRingIntermediate, new[] {
                "righthandring2",
                "ringfinger2_right",
                "ring2_r", "ring2.r",
            }),
            (HumanBodyBones.RightRingDistal, new[] {
                "righthandring3",
                "ringfinger3_right",
                "ring3_r", "ring3.r",
            }),
            (HumanBodyBones.RightLittleProximal, new[] {
                "righthandpinky1",
                "pinkyfinger1_right",
                "pinky1_r", "pinky1.r",
            }),
            (HumanBodyBones.RightLittleIntermediate, new[] {
                "righthandpinky2",
                "pinkyfinger2_right",
                "pinky2_r", "pinky2.r",
            }),
            (HumanBodyBones.RightLittleDistal, new[] {
                "righthandpinky3",
                "pinkyfinger3_right",
                "pinky3_r", "pinky3.r",
            }),
        };
    }
}
