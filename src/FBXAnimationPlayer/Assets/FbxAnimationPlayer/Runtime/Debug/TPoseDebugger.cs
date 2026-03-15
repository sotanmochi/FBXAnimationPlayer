using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FbxAnimationPlayer
{
    public class TPoseDebugger : MonoBehaviour
    {
        public event Action<TPoseStepSnapshot> OnSnapshotChanged;
        public event Action<Dictionary<HumanBodyBones, Transform>, Dictionary<HumanBodyBones, Transform>> OnBoneMapReady;

        private TPoseStepExecutor _executor;
        private TPoseDebugVisualizer _visualizer;
        private GameObject _fbxRootObject;
        private GameObject _skeletonObject;
        private Dictionary<HumanBodyBones, Transform> _fbxBoneMap;
        private Dictionary<HumanBodyBones, Transform> _skeletonBoneMap;

        public int TotalSteps => _executor?.TotalSteps ?? 0;
        public int CurrentStep => _executor?.CurrentStep ?? -1;
        public IReadOnlyList<TPoseStepSnapshot> Snapshots => _executor?.Snapshots;
        public bool IsLoaded => _executor != null && _skeletonBoneMap != null;

        public BoneNameMappingConfig BoneNameMappingConfig { get; set; }

        void OnDestroy()
        {
            Cleanup();
        }

        public TPoseDebugVisualizer GetVisualizer() => _visualizer;

        public void StepForward()
        {
            if (_executor == null || _executor.CurrentStep >= _executor.TotalSteps - 1) return;
            _executor.ExecuteNextStep();
            NotifySnapshotChanged();
        }

        public void StepBackward()
        {
            if (_executor == null || _executor.CurrentStep <= 0) return;
            _executor.GoToStep(_executor.CurrentStep - 1);
            NotifySnapshotChanged();
        }

        public void GoToStep(int stepIndex)
        {
            if (_executor == null) return;
            _executor.GoToStep(stepIndex);
            NotifySnapshotChanged();
        }

        public void RunAll()
        {
            if (_executor == null) return;
            _executor.ExecuteToStep(_executor.TotalSteps - 1);
            NotifySnapshotChanged();
        }

        public void ResetToInitial()
        {
            if (_executor == null) return;
            _executor.Reset();
            _executor.ExecuteNextStep(); // re-capture initial state
            NotifySnapshotChanged();
        }

        public async UniTask LoadForDebug(string filePath, CancellationToken ct)
        {
            byte[] bytes = await File.ReadAllBytesAsync(filePath, ct);
            await LoadForDebug(new MemoryStream(bytes), ct);
        }

        public async UniTask LoadForDebug(Stream stream, CancellationToken ct)
        {
            Cleanup();

            // Use FbxAnimationImporter's internal method for FBX import
            var fbxImportResult = await FbxAnimationImporter.ImportFbxAsync(stream, ct);
            if (fbxImportResult == null || fbxImportResult.GameObject == null)
            {
                Debug.LogError("[TPoseDebugger] FBX import failed or was canceled.");
                return;
            }

            _fbxRootObject = fbxImportResult.GameObject;
            _fbxRootObject.transform.SetParent(transform);
            _fbxRootObject.transform.localPosition = Vector3.zero;
            _fbxRootObject.transform.localRotation = Quaternion.identity;
            _fbxRootObject.transform.localScale = Vector3.one;
            _fbxRootObject.name = "FBX";
    
            FbxAnimationImporter.RemoveLegacyAnimationComponent(_fbxRootObject);
            FbxAnimationImporter.RemoveCamerasAndLights(_fbxRootObject); // Remove unnecessary objects

            // Get first animation clip and sample frame 0
            AnimationClip firstClip = null;
            if (fbxImportResult.Animations != null)
            {
                foreach (var clip in fbxImportResult.Animations.Values)
                {
                    if (clip != null) { firstClip = clip; break; }
                }
            }

            if (firstClip == null)
            {
                Debug.LogError("[TPoseDebugger] No animation clips found.");
                return;
            }

            firstClip.SampleAnimation(_fbxRootObject, 0f);

            // Use FbxAnimationImporter's internal method for bone name mapping resolution
            FbxAnimationImporter.ResolveBoneNameMappingConfig(BoneNameMappingConfig,
                out var prefixesToStrip, out var boneNamePatterns);

            _fbxBoneMap = HumanAvatarSkeletonUtility.CreateBoneTransformMap(
                _fbxRootObject, prefixesToStrip, boneNamePatterns);

            if (!_fbxBoneMap.TryGetValue(HumanBodyBones.Hips, out var originalHips))
            {
                Debug.LogError("[TPoseDebugger] Hips bone not found.");
                return;
            }

            // Use FbxAnimationImporter's internal method for skeleton cloning
            _skeletonObject = new GameObject("HumanAvatarSkeleton_Debug");
            _skeletonObject.transform.SetParent(transform);
            FbxAnimationImporter.CloneSkeletonHierarchy(
                originalHips, _fbxBoneMap, _skeletonObject.transform, out _skeletonBoneMap);

            // Initialize step executor (ApplyTPose is NOT called here; stepped instead)
            _executor = new TPoseStepExecutor();
            _executor.Initialize(_skeletonBoneMap);

            // Setup visualizer
            _visualizer = _skeletonObject.AddComponent<TPoseDebugVisualizer>();
            _visualizer.SetFbxSkeletonRoot(originalHips);

            // Take initial snapshot
            _executor.ExecuteNextStep();
            NotifySnapshotChanged();

            OnBoneMapReady?.Invoke(_fbxBoneMap, _skeletonBoneMap);
        }

        private void NotifySnapshotChanged()
        {
            if (_executor?.Snapshots == null || _executor.Snapshots.Count == 0) return;

            var current = _executor.Snapshots[_executor.CurrentStep];
            var previous = _executor.CurrentStep > 0 ? _executor.Snapshots[_executor.CurrentStep - 1] : null;

            _visualizer?.SetSnapshot(current, previous);
            OnSnapshotChanged?.Invoke(current);
        }

        private void Cleanup()
        {
            if (_fbxRootObject != null) Destroy(_fbxRootObject);
            if (_skeletonObject != null) Destroy(_skeletonObject);
            _fbxRootObject = null;
            _skeletonObject = null;
            _fbxBoneMap = null;
            _skeletonBoneMap = null;
            _executor = null;
            _visualizer = null;
        }
    }
}
