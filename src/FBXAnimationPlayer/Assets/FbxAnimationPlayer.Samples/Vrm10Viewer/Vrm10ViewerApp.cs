using System.IO;
using UnityEngine;

namespace FbxAnimationPlayer.Samples
{
    public sealed class Vrm10ViewerApp : MonoBehaviour
    {
        [SerializeField] private string _defaultVrmModel = "VRM/AvatarSample_A.vrm";
        [SerializeField] private VrmLoader _vrmLoader;
        [SerializeField] private FbxLoader _fbxLoader;
        [SerializeField] private AnimationControlPanel _animationControlPanel;

        [ReadOnly, SerializeField]
        private FbxAnimationController _animationController;

        [ReadOnly, SerializeField]
        private FbxMotionActor _motionActor;

        [ReadOnly, SerializeField]
        private GameObject _target;

        private HumanPose _humanPose;
        private HumanPoseHandler _targetPoseHandler;

        void Start()
        {
            _fbxLoader.FbxAnimationLoaded += OnFbxAnimationLoaded;
            _vrmLoader.ModelLoaded += OnVrmModelLoaded;
            _vrmLoader.LoadVrmModel(Path.Combine(Application.streamingAssetsPath, _defaultVrmModel));
        }

        void OnDestroy()
        {
            if (_fbxLoader != null) _fbxLoader.FbxAnimationLoaded -= OnFbxAnimationLoaded;
            if (_vrmLoader != null) _vrmLoader.ModelLoaded -= OnVrmModelLoaded;

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
        }

        private void OnVrmModelLoaded(GameObject targetObject)
        {
            if (targetObject != null && targetObject.TryGetComponent<Animator>(out var animator))
            {
                _targetPoseHandler?.Dispose();
                _targetPoseHandler = null;

                UnityEngine.Object.Destroy(_target);
                _target = null;

                _targetPoseHandler = new HumanPoseHandler(animator.avatar, animator.transform);
                _target = targetObject;
            }
        }
    }
}
