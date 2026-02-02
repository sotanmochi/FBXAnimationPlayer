using System.IO;
using UnityEngine;

namespace FbxAnimationPlayer.Samples
{
    public sealed class Vrm10ViewerApp : MonoBehaviour
    {
        [SerializeField] private string _defaultVrmModel = "VRM/AvatarSample_A.vrm";
        [SerializeField] private VrmLoader _vrmLoader;
        [SerializeField] private FbxLoader _fbxLoader;

        [ReadOnly, SerializeField]
        private FbxMotionActor _fbxMotionActor;

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

        void LateUpdate()
        {
            if (_fbxMotionActor == null || _targetPoseHandler == null)
            {
                return;
            }

            if (_fbxMotionActor.TryGetHumanPose(ref _humanPose))
            {
                _targetPoseHandler.SetHumanPose(ref _humanPose);
            }
        }

        private void OnFbxAnimationLoaded(ImportResult importResult)
        {
            var previousActorParent = _fbxMotionActor?.transform.parent.gameObject;
            if (previousActorParent != null)
            {
                UnityEngine.Object.Destroy(previousActorParent);
            }
            _fbxMotionActor = importResult.FbxMotionActor;
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
