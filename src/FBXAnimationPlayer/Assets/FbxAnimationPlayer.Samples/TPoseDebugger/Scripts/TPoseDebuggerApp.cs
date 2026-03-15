using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace FbxAnimationPlayer.Samples
{
    /// <summary>
    /// TPoseDebugger sample scene entry point.
    ///
    /// Required scene setup:
    /// - Camera with OrbitCamera, OrbitCameraController, PointerInputHandler
    /// - GameObject with TPoseDebugger
    /// - GameObject with UIDocument (UXML: TPoseDebugger.uxml) + TPoseDebuggerUI
    /// </summary>
    public class TPoseDebuggerApp : MonoBehaviour
    {
        [SerializeField] private TPoseDebugger _debugger;
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private OrbitCameraController _cameraController;
        [SerializeField] private OrbitCamera _orbitCamera;

        private IFilePicker _filePicker;
        private CancellationTokenSource _cts;

        void Start()
        {
            _cts = new CancellationTokenSource();

            var factory = new FilePickerFactory();
            _filePicker = factory.Create();

            var root = _uiDocument.rootVisualElement;
            var loadBtn = root.Q<Button>("load-fbx-button");
            if (loadBtn != null)
            {
                loadBtn.clicked += OnLoadClicked;
            }

            var resetCameraBtn = root.Q<Button>("reset-camera-button");
            if (resetCameraBtn != null)
            {
                resetCameraBtn.clicked += () =>
                {
                    if (_cameraController == null)
                    {
                        Debug.LogWarning("[TPoseDebuggerApp] OrbitCameraController is not assigned.");
                        return;
                    }
                    _cameraController.ResetCamera();
                };
            }

            if (_debugger != null)
            {
                _debugger.OnBoneMapReady += OnBoneMapReady;
            }
        }

        void OnDestroy()
        {
            if (_debugger != null)
            {
                _debugger.OnBoneMapReady -= OnBoneMapReady;
            }
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private void OnBoneMapReady(
            Dictionary<HumanBodyBones, Transform> fbxBoneMap,
            Dictionary<HumanBodyBones, Transform> skeletonBoneMap)
        {
            if (_orbitCamera == null) return;

            if (skeletonBoneMap.TryGetValue(HumanBodyBones.Hips, out var hips) && hips != null)
            {
                var visualizer = _debugger.GetVisualizer();
                _orbitCamera.LookAtTarget = visualizer != null
                    ? visualizer.GetOffsetPosition(hips.position)
                    : hips.position;
            }
        }

        private void OnLoadClicked()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("File picker is not supported on WebGL");
#else
            _filePicker.PickFile("Open FBX File", new[] { "fbx" }, async path =>
            {
                if (string.IsNullOrEmpty(path)) return;
                await UniTask.SwitchToMainThread();
                await _debugger.LoadForDebug(path, _cts.Token);
            });
#endif
        }
    }
}
