using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace FbxAnimationPlayer.Samples
{
    public sealed class Vrm10ViewerApp : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private OrbitCameraController _orbitCameraController;
        [SerializeField] private string _defaultVrmModel = "VRM/Sample_Alpha_PerfectSync.vrm";
        [SerializeField] private bool _autoPlayFbxAnimation = true;

        [ReadOnly, SerializeField]
        private FbxAnimationController _animationController;

        [ReadOnly, SerializeField]
        private FbxMotionActor _motionActor;

        [ReadOnly, SerializeField]
        private GameObject _target;

        private readonly VrmLoader _vrmLoader = new(new FilePickerFactory());
        private readonly FbxLoader _fbxLoader = new(new FilePickerFactory());
        private readonly AnimationControlPanel _animationControlPanel = new();
        private readonly CameraBackgroundColorController _bgColorController = new();
        private readonly SettingsPanel _settingsPanel = new();

        private HumanPose _humanPose;
        private HumanPoseHandler _targetPoseHandler;

        void Start()
        {
            var root = _uiDocument.rootVisualElement;
            _vrmLoader.Setup(root);
            _fbxLoader.Setup(root);
            _animationControlPanel.Setup(root);

            _bgColorController.SetCamera(Camera.main);
            _settingsPanel.Setup(root, _bgColorController);
            _settingsPanel.FoVChanged += OnFoVChanged;
            _settingsPanel.ResetCameraRequested += OnResetCameraRequested;

            _fbxLoader.FbxAnimationLoaded += OnFbxAnimationLoaded;
            _vrmLoader.ModelLoaded += OnVrmModelLoaded;
            _vrmLoader.LoadVrmModel(Path.Combine(Application.streamingAssetsPath, _defaultVrmModel));
        }

        void OnDestroy()
        {
            _fbxLoader.FbxAnimationLoaded -= OnFbxAnimationLoaded;
            _vrmLoader.ModelLoaded -= OnVrmModelLoaded;
            _settingsPanel.FoVChanged -= OnFoVChanged;
            _settingsPanel.ResetCameraRequested -= OnResetCameraRequested;

            _settingsPanel.Dispose();
            _animationControlPanel.Dispose();
            _fbxLoader.Dispose();
            _vrmLoader.Dispose();

            _targetPoseHandler?.Dispose();
            _targetPoseHandler = null;
        }

        void LateUpdate()
        {
            if (_motionActor == null || _targetPoseHandler == null)
            {
                return;
            }

            if (_motionActor.TryGetHumanPose(ref _humanPose))
            {
                _targetPoseHandler.SetHumanPose(ref _humanPose);
            }
        }

        private void OnFbxAnimationLoaded(ImportResult importResult)
        {
            _animationController?.Dispose();
            _motionActor?.Dispose();

            _animationController = importResult.AnimationController;
            _motionActor = importResult.MotionActor;

            _animationControlPanel.Bind(_animationController);

            if (_autoPlayFbxAnimation)
            {
                _animationController.Play();
            }
        }

        private void OnVrmModelLoaded(GameObject targetObject)
        {
            if (targetObject != null && targetObject.TryGetComponent<Animator>(out var animator))
            {
                _targetPoseHandler?.Dispose();
                _targetPoseHandler = null;

                UnityObjectDestroyer.DestroyRuntimeOrEditor(_target);
                _target = null;

                _targetPoseHandler = new HumanPoseHandler(animator.avatar, animator.transform);
                _target = targetObject;
            }
        }

        private void OnFoVChanged(float fov)
        {
            _orbitCameraController.SetFieldOfView(fov);
        }

        private void OnResetCameraRequested()
        {
            _orbitCameraController.ResetCamera();
        }

    }
}
