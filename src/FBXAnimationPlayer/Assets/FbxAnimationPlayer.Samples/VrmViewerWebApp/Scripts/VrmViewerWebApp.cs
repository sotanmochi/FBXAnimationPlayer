using System.IO;
using UnityEngine;
#if FBXANIMPLAYER_R3_SUPPORT
using R3;
#elif FBXANIMPLAYER_UNIRX_SUPPORT
using UniRx;
#endif

namespace FbxAnimationPlayer.Samples
{
    public sealed class VrmViewerWebApp : MonoBehaviour
    {
        [SerializeField] private WebAppMessageBus _messageBus;
        [SerializeField] private OrbitCameraController _orbitCameraController;
        [SerializeField] private string _defaultVrmModel = "VRM/AvatarSample_A.vrm";
        [SerializeField] private bool _autoPlayFbxAnimation = true;

#if FBXANIMPLAYER_R3_SUPPORT || FBXANIMPLAYER_UNIRX_SUPPORT
        private readonly CompositeDisposable _disposables = new();
        private CompositeDisposable _animEventDisposables;
#endif

        private readonly VrmLoader _vrmLoader = new(filePickerFactory: null);
        private readonly FbxLoader _fbxLoader = new(filePickerFactory: null);
        private readonly CameraBackgroundColorController _bgColorController = new();

        private FbxAnimationController _animController;
        private FbxMotionActor _motionActor;
        private GameObject _target;

        private HumanPose _humanPose;
        private HumanPoseHandler _targetPoseHandler;

        private void Awake()
        {
            _vrmLoader.ModelLoaded        += OnVrmModelLoaded;
            _fbxLoader.FbxAnimationLoaded += OnFbxAnimationLoaded;
        }

        private void OnDestroy()
        {
#if FBXANIMPLAYER_R3_SUPPORT || FBXANIMPLAYER_UNIRX_SUPPORT
            _disposables.Dispose();
#else
            _messageBus.MessageReceived -= OnMessageReceived;
#endif

            _vrmLoader.ModelLoaded        -= OnVrmModelLoaded;
            _fbxLoader.FbxAnimationLoaded -= OnFbxAnimationLoaded;

            UnsubscribeAnimationEvents();

            _fbxLoader.Dispose();
            _vrmLoader.Dispose();
            _targetPoseHandler?.Dispose();
        }

        private void Start()
        {
            _bgColorController.SetCamera(Camera.main);

#if FBXANIMPLAYER_R3_SUPPORT || FBXANIMPLAYER_UNIRX_SUPPORT
            SubscribeMessageBusEvents();
#else
            _messageBus.MessageReceived += OnMessageReceived;
#endif

            var defaultPath = Path.Combine(Application.streamingAssetsPath, _defaultVrmModel);
            _vrmLoader.LoadVrmModel(defaultPath);

            _messageBus.Emit("app/ready");
        }

        private void LateUpdate()
        {
            if (_motionActor == null || _targetPoseHandler == null) return;

            if (_motionActor.TryGetHumanPose(ref _humanPose))
            {
                _targetPoseHandler.SetHumanPose(ref _humanPose);
            }
        }

        private void OnVrmModelLoaded(GameObject targetObject)
        {
            if (targetObject == null || !targetObject.TryGetComponent<Animator>(out var animator))
            {
                _messageBus.Emit("vrm/error", "{\"message\":\"Animator not found on loaded VRM.\"}");
                return;
            }

            _targetPoseHandler?.Dispose();
            UnityObjectDestroyer.DestroyRuntimeOrEditor(_target);

            _targetPoseHandler = new HumanPoseHandler(animator.avatar, animator.transform);
            _target = targetObject;

            _messageBus.Emit("vrm/loaded", "{\"success\":true}");
        }

        private void OnFbxAnimationLoaded(ImportResult result)
        {
            if (!result.IsSuccess)
            {
                var escaped = result.ErrorMessage?.Replace("\"", "\\\"") ?? "Unknown error";
                _messageBus.Emit("fbx/error", $"{{\"message\":\"{escaped}\"}}");
                return;
            }

            UnsubscribeAnimationEvents();
            _animController?.Dispose();
            _motionActor?.Dispose();

            _animController = result.AnimationController;
            _motionActor    = result.MotionActor;

            SubscribeAnimationEvents();

            var clipCount = result.AnimationClips?.Count ?? 0;
            _messageBus.Emit("fbx/loaded", $"{{\"success\":true,\"clipCount\":{clipCount}}}");

            if (_autoPlayFbxAnimation)
            {
                _animController.Play();
            }
        }

#if FBXANIMPLAYER_R3_SUPPORT || FBXANIMPLAYER_UNIRX_SUPPORT

        private void SubscribeMessageBusEvents()
        {
            _messageBus.OnMessageReceivedAsObservable<UrlPayload>("vrm/load")
                .Subscribe(p => _vrmLoader.LoadVrmModel(p.url))
                .AddTo(_disposables);

            _messageBus.OnMessageReceivedAsObservable<UrlPayload>("fbx/load")
                .Subscribe(p => _fbxLoader.LoadFbxAnimation(p.url))
                .AddTo(_disposables);

            _messageBus.OnMessageReceivedAsObservable()
                .Where(m => m.type == "animation/play")
                .Subscribe(_ => _animController?.Play())
                .AddTo(_disposables);

            _messageBus.OnMessageReceivedAsObservable()
                .Where(m => m.type == "animation/pause")
                .Subscribe(_ => _animController?.Pause())
                .AddTo(_disposables);

            _messageBus.OnMessageReceivedAsObservable()
                .Where(m => m.type == "animation/stop")
                .Subscribe(_ => _animController?.Stop())
                .AddTo(_disposables);

            _messageBus.OnMessageReceivedAsObservable<SeekPayload>("animation/seek")
                .Subscribe(p => _animController?.SeekNormalized(p.normalizedTime))
                .AddTo(_disposables);

            _messageBus.OnMessageReceivedAsObservable<LoopPayload>("animation/setLooping")
                .Subscribe(p => { if (_animController != null) _animController.IsLooping = p.enabled; })
                .AddTo(_disposables);

            _messageBus.OnMessageReceivedAsObservable<SpeedPayload>("animation/setSpeed")
                .Subscribe(p => { if (_animController != null) _animController.Speed = p.speed; })
                .AddTo(_disposables);

            _messageBus.OnMessageReceivedAsObservable<BackgroundColorPayload>("background/setColor")
                .Subscribe(p => _bgColorController.SetColorFromRGB(p.r, p.g, p.b))
                .AddTo(_disposables);

            _messageBus.OnMessageReceivedAsObservable<FoVPayload>("camera/setFoV")
                .Subscribe(p => { _orbitCameraController.SetFieldOfView(p.fov); })
                .AddTo(_disposables);

            _messageBus.OnMessageReceivedAsObservable()
                .Where(m => m.type == "camera/reset")
                .Subscribe(_ => { _orbitCameraController.ResetCamera(); })
                .AddTo(_disposables);
        }

        private void SubscribeAnimationEvents()
        {
            if (_animController == null) return;

            _animEventDisposables = new CompositeDisposable();

            _animController.OnStateChangedAsObservable()
                .Subscribe(state =>
                {
                    var stateStr = state switch
                    {
                        AnimationPlayState.Playing => "playing",
                        AnimationPlayState.Paused  => "paused",
                        _                          => "stopped",
                    };
                    _messageBus.Emit("animation/stateChanged", $"{{\"state\":\"{stateStr}\"}}");
                })
                .AddTo(_animEventDisposables);

            _animController.OnTimeUpdatedAsObservable()
                .Subscribe(currentTime =>
                {
                    var payload = JsonUtility.ToJson(new TimeUpdatePayload
                    {
                        current = currentTime,
                        duration = _animController?.Duration ?? 0f,
                    });
                    _messageBus.Emit("animation/timeUpdated", payload);
                })
                .AddTo(_animEventDisposables);
        }

        private void UnsubscribeAnimationEvents()
        {
            _animEventDisposables?.Dispose();
            _animEventDisposables = null;
        }

#else

        private void OnMessageReceived(Message msg)
        {
            switch (msg.type)
            {
                case "vrm/load":
                    var vrmPayload = DeserializePayload<UrlPayload>(msg.payload);
                    _vrmLoader.LoadVrmModel(vrmPayload.url);
                    break;

                case "fbx/load":
                    var fbxPayload = DeserializePayload<UrlPayload>(msg.payload);
                    _fbxLoader.LoadFbxAnimation(fbxPayload.url);
                    break;

                case "animation/play":
                    _animController?.Play();
                    break;

                case "animation/pause":
                    _animController?.Pause();
                    break;

                case "animation/stop":
                    _animController?.Stop();
                    break;

                case "animation/seek":
                    var seekPayload = DeserializePayload<SeekPayload>(msg.payload);
                    _animController?.SeekNormalized(seekPayload.normalizedTime);
                    break;

                case "animation/setLooping":
                    var loopPayload = DeserializePayload<LoopPayload>(msg.payload);
                    if (_animController != null) _animController.IsLooping = loopPayload.enabled;
                    break;

                case "animation/setSpeed":
                    var speedPayload = DeserializePayload<SpeedPayload>(msg.payload);
                    if (_animController != null) _animController.Speed = speedPayload.speed;
                    break;

                case "background/setColor":
                    var bgPayload = DeserializePayload<BackgroundColorPayload>(msg.payload);
                    _bgColorController.SetColorFromRGB(bgPayload.r, bgPayload.g, bgPayload.b);
                    break;

                case "camera/setFoV":
                    var fovPayload = DeserializePayload<FoVPayload>(msg.payload);
                    _orbitCameraController.SetFieldOfView(fovPayload.fov);
                    break;

                case "camera/reset":
                    _orbitCameraController.ResetCamera();
                    break;
            }
        }

        private static TPayload DeserializePayload<TPayload>(string payload) where TPayload : new()
        {
            if (string.IsNullOrEmpty(payload)) return new TPayload();
            try   { return JsonUtility.FromJson<TPayload>(payload); }
            catch { return new TPayload(); }
        }

        private void SubscribeAnimationEvents()
        {
            if (_animController == null) return;
            _animController.StateChanged += OnAnimationStateChanged;
            _animController.TimeUpdated  += OnAnimationTimeUpdated;
        }

        private void UnsubscribeAnimationEvents()
        {
            if (_animController == null) return;
            _animController.StateChanged -= OnAnimationStateChanged;
            _animController.TimeUpdated  -= OnAnimationTimeUpdated;
        }

        private void OnAnimationStateChanged(AnimationPlayState state)
        {
            var stateStr = state switch
            {
                AnimationPlayState.Playing => "playing",
                AnimationPlayState.Paused  => "paused",
                _                          => "stopped",
            };
            _messageBus.Emit("animation/stateChanged", $"{{\"state\":\"{stateStr}\"}}");
        }

        private void OnAnimationTimeUpdated(float currentTime)
        {
            var payload = JsonUtility.ToJson(new TimeUpdatePayload
            {
                current = currentTime,
                duration = _animController?.Duration ?? 0f,
            });
            _messageBus.Emit("animation/timeUpdated", payload);
        }

#endif
    }
}
