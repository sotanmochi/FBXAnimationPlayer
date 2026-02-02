using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using FBXImporter;
using FBXImporter.MaterialConverters;
using UnityEngine;

namespace FbxAnimationPlayer
{
    public class ImportResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public FbxMotionActor FbxMotionActor { get; set; }
        // TODO
    }

    public static class FbxAnimationImporter
    {
        public static async UniTask<ImportResult> LoadAsync(
            Stream stream, CancellationToken cancellationToken, IProgress<float> progress = null)
        {
            try
            {
                if (stream == null || stream.Length == 0)
                {
                    return new ImportResult()
                    {
                        IsSuccess = false,
                        ErrorMessage = "Stream is null or empty."
                    };
                }

                var result = await ImportFbxAsync(stream, cancellationToken, progress);
                if (result == null)
                {
                    return new ImportResult()
                    {
                        IsSuccess = false,
                        ErrorMessage = "FBX import was canceled."
                    };
                }

                return CreateAnimationImportResult(result);
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log("Loading task was canceled.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"Failed to load FBX animation from stream: {ex.Message}");
            }

            return new ImportResult()
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred during FBX animation loading."
            };
        }

        public static async UniTask<ImportResult> LoadAsync(
            string filePath, CancellationToken cancellationToken, IProgress<float> progress = null)
        {
            try
            {
                if (IsInvalidFilePath(filePath, out var errorMessage))
                {
                    return new ImportResult()
                    {
                        IsSuccess = false,
                        ErrorMessage = errorMessage
                    };
                }

                var result = await ImportFbxAsync(filePath, cancellationToken, progress);
                if (result == null)
                {
                    return new ImportResult()
                    {
                        IsSuccess = false,
                        ErrorMessage = "FBX import was canceled."
                    };
                }

                return CreateAnimationImportResult(result);
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log("Loading task was canceled.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"Failed to load FBX animation from '{filePath}': {ex.Message}");
            }

            return new ImportResult()
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred during FBX animation loading."
            };
        }

        private static bool IsInvalidFilePath(string filePath, out string errorMessage)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                errorMessage = "File path is null or empty.";
                return true;
            }

            if (!System.IO.File.Exists(filePath))
            {
                errorMessage = $"File not found at path: {filePath}";
                return true;
            }

            errorMessage = null;
            return false;
        }

        private static async UniTask<FBXImporter.Importer.ImportResult> ImportFbxAsync(
            Stream stream, CancellationToken cancellationToken, IProgress<float> progress = null)
        {
            var importResult = new FBXImporter.Importer.ImportResult();

            var loadOpts = Importer.DefaultLoadOpts;
            loadOpts.ignore_animation = false;
            loadOpts.ignore_geometry = true;
            loadOpts.ignore_embedded = true;

            var enumerator = Importer.ImportFromStreamAsync(
                importResult: importResult,
                stream: stream,
                loadOpts: loadOpts,
                bakeOpts: default,
                materialConverter: new StandardMaterialConverter()
            );

            while (enumerator.MoveNext())
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(importResult.LoadingPercentage);
                await UniTask.Yield(cancellationToken);
            }

            if (importResult.LoadingCanceled)
            {
                return null;
            }

            progress?.Report(1.0f);
            return importResult;
        }

        private static async UniTask<FBXImporter.Importer.ImportResult> ImportFbxAsync(
            string filePath, CancellationToken cancellationToken, IProgress<float> progress = null)
        {
            var importResult = new FBXImporter.Importer.ImportResult();

            var loadOpts = Importer.DefaultLoadOpts;
            loadOpts.ignore_animation = false;
            loadOpts.ignore_geometry = true;
            loadOpts.ignore_embedded = true;

            var enumerator = Importer.ImportFromFileAsync(
                importResult: importResult,
                filename: filePath,
                loadOpts: loadOpts,
                bakeOpts: default,
                materialConverter: new StandardMaterialConverter()
            );

            while (enumerator.MoveNext())
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(importResult.LoadingPercentage);
                await UniTask.Yield(cancellationToken);
            }

            if (importResult.LoadingCanceled)
            {
                return null;
            }

            progress?.Report(1.0f);
            return importResult;
        }

        private static ImportResult CreateAnimationImportResult(FBXImporter.Importer.ImportResult fbxImportResult)
        {
            var fbxResultRootObject = fbxImportResult.GameObject;
            if (fbxResultRootObject == null)
            {
                return new ImportResult()
                {
                    IsSuccess = false,
                    ErrorMessage = "The root GameObject was not found in the FBX import result."
                };
            }

            RemoveCamerasAndLights(fbxResultRootObject); // Remove unnecessary objects

            var gameObject = new GameObject("FBXAnimation");
            fbxResultRootObject.transform.SetParent(gameObject.transform);
            fbxResultRootObject.transform.localPosition = Vector3.zero;
            fbxResultRootObject.transform.localRotation = Quaternion.identity;
            fbxResultRootObject.transform.localScale = Vector3.one;

            var animation = fbxResultRootObject.GetComponent<Animation>();
            var animationClips = new List<AnimationClip>();
            if (fbxImportResult.Animations != null)
            {
                foreach (var clip in fbxImportResult.Animations.Values)
                {
                    if (clip != null)
                    {
                        animationClips.Add(clip);
                    }
                }
            }

            if (animationClips.Count <= 0)
            {
                return new ImportResult()
                {
                    IsSuccess = false,
                    ErrorMessage = "No animation clips found in the FBX import result."
                };
            }

            var skeleton = CreateHumanAvatarSkeleton(
                fbxResultRootObject,
                animationClips[0],
                out var fbxBoneMap,
                out var skeletonBoneMap);
            skeleton.transform.SetParent(gameObject.transform);

            var humanAvatar = HumanAvatarBuilder.BuildHumanAvatar(skeleton, skeletonBoneMap);
            var motionActor = skeleton.AddComponent<FbxMotionActor>();
            motionActor.SetHumanAvatar(humanAvatar, skeleton.transform);

            var synchronizer = skeleton.AddComponent<HumanBoneTransformSynchronizer>();
            synchronizer.Setup(fbxBoneMap, skeletonBoneMap);

            return new ImportResult()
            {
                IsSuccess = true,
                FbxMotionActor = motionActor,
                // TODO
            };
        }

        private static void RemoveCamerasAndLights(GameObject root)
        {
            var cameras = root.GetComponentsInChildren<Camera>(true);
            foreach (var camera in cameras)
            {
                UnityEngine.Object.Destroy(camera.gameObject);
            }

            var lights = root.GetComponentsInChildren<Light>(true);
            foreach (var light in lights)
            {
                UnityEngine.Object.Destroy(light.gameObject);
            }
        }

        private static GameObject CreateHumanAvatarSkeleton(
            GameObject fbxRootObject,
            AnimationClip animationClip,
            out Dictionary<HumanBodyBones, Transform> fbxBoneMap,
            out Dictionary<HumanBodyBones, Transform> skeletonBoneMap)
        {
            // Sample the first frame to get the localPosition of each bone.
            // 各ボーンのlocalPositionを取得するため、最初のフレームをサンプリングする。
            animationClip.SampleAnimation(fbxRootObject, 0f);

            fbxBoneMap = HumanAvatarSkeletonUtility.CreateBoneTransformMap(fbxRootObject);
            skeletonBoneMap = null;

            var skeleton = new GameObject("HumanAvatarSkeleton");
            if (fbxBoneMap.TryGetValue(HumanBodyBones.Hips, out var originalHips))
            {
                CloneSkeletonHierarchy(originalHips, fbxBoneMap, skeleton.transform, out skeletonBoneMap);
                HumanAvatarSkeletonUtility.ApplyTPose(skeletonBoneMap, adjustHeightToGround: true);
            }
            else
            {
                DebugLogger.LogError($"Hips bone not found in FBX skeleton.");
                UnityEngine.Object.Destroy(skeleton);
                skeleton = null;
            }

            return skeleton;
        }

        private static void CloneSkeletonHierarchy(
            Transform originalHips,
            Dictionary<HumanBodyBones, Transform> originalBoneMap,
            Transform clonedSkeletonRoot,
            out Dictionary<HumanBodyBones, Transform> clonedSkeletonBoneMap)
        {
            clonedSkeletonBoneMap = new Dictionary<HumanBodyBones, Transform>();

            var originalToClone = new Dictionary<Transform, Transform>();
            CloneRecursive(originalHips, clonedSkeletonRoot, originalToClone);

            foreach (var kvp in originalBoneMap)
            {
                var humanBone = kvp.Key;
                var originalBoneTransform = kvp.Value;
                if (originalToClone.TryGetValue(originalBoneTransform, out var clonedBoneTransform))
                {
                    clonedSkeletonBoneMap[humanBone] = clonedBoneTransform;
                }
            }
        }

        private static void CloneRecursive(
            Transform original,
            Transform clonedParent,
            Dictionary<Transform, Transform> originalToClone)
        {
            var clone = new GameObject(original.name).transform;
            originalToClone[original] = clone;

            clone.SetParent(clonedParent, false);
            clone.localPosition = original.localPosition;
            clone.localScale = original.localScale;

            // The local rotation will be updated later by HumanAvatarSkeletonUtility.ApplyTPose().
            clone.localRotation = Quaternion.identity;

            // Clone children recursively (attach to clone, not to clonedParent)
            foreach (Transform child in original)
            {
                CloneRecursive(child, clone, originalToClone);
            }
        }
    }
}
