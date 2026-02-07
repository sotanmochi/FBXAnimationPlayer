using UnityEngine;
using UnityEngine.UI;

namespace FbxAnimationPlayer.Samples
{
    public sealed class AnimationControlPanel : MonoBehaviour
    {
        [SerializeField] private bool _autoPlay = true;
        [SerializeField] private Button _playPauseButton;
        [SerializeField] private Text _playPauseLabel;
        [SerializeField] private Button _stopButton;
        [SerializeField] private Toggle _loopToggle;
        [SerializeField] private Slider _seekBar;
        [SerializeField] private Text _timeLabel;

        private FbxAnimationController _animationController;

        void Awake()
        {
            _playPauseButton.onClick.AddListener(OnPlayPauseClicked);
            _stopButton.onClick.AddListener(OnStopClicked);
            _loopToggle.onValueChanged.AddListener(OnLoopToggleValueChanged);
            _seekBar.onValueChanged.AddListener(OnSeekBarValueChanged);
            SetInteractable(false);
        }

        void OnDestroy()
        {
            Unbind();
            _loopToggle.onValueChanged.RemoveAllListeners();
            _playPauseButton.onClick.RemoveAllListeners();
            _stopButton.onClick.RemoveAllListeners();
            _seekBar.onValueChanged.RemoveAllListeners();
        }

        public void Bind(FbxAnimationController controller)
        {
            Unbind();

            _animationController = controller;
            _animationController.StateChanged += OnStateChanged;
            _animationController.TimeUpdated += OnTimeUpdated;

            _seekBar.SetValueWithoutNotify(0f);
            UpdateTimeLabel(0f, _animationController.Duration);
            UpdatePlayPauseLabel(_animationController.State);
            SetInteractable(true);

            if (_autoPlay)
            {
                _animationController.Play();
            }
        }

        private void Unbind()
        {
            if (_animationController == null) return;

            _animationController.StateChanged -= OnStateChanged;
            _animationController.TimeUpdated -= OnTimeUpdated;
            _animationController = null;

            SetInteractable(false);
        }

        private void OnPlayPauseClicked()
        {
            if (_animationController == null) return;

            switch (_animationController.State)
            {
                case AnimationPlayState.Stopped:
                case AnimationPlayState.Paused:
                    _animationController.Play();
                    break;
                case AnimationPlayState.Playing:
                    _animationController.Pause();
                    break;
            }
        }

        private void OnStopClicked()
        {
            _animationController?.Stop();
        }

        private void OnLoopToggleValueChanged(bool isOn)
        {
            if (_animationController == null) return;
            _animationController.IsLooping = isOn;
        }

        private void OnSeekBarValueChanged(float value)
        {
            _animationController?.SeekNormalized(value);
        }

        private void OnStateChanged(AnimationPlayState state)
        {
            UpdatePlayPauseLabel(state);
        }

        private void OnTimeUpdated(float currentTime)
        {
            if (_animationController == null) return;
            _seekBar.SetValueWithoutNotify(_animationController.NormalizedTime);
            UpdateTimeLabel(currentTime, _animationController.Duration);
        }

        private void UpdatePlayPauseLabel(AnimationPlayState state)
        {
            _playPauseLabel.text = state == AnimationPlayState.Playing ? "Pause" : "Play";
        }

        private void UpdateTimeLabel(float currentTime, float duration)
        {
            if (_timeLabel == null) return;
            _timeLabel.text = $"{currentTime:F2} / {duration:F2} [s]";
        }

        private void SetInteractable(bool interactable)
        {
            _playPauseButton.interactable = interactable;
            _stopButton.interactable = interactable;
            _seekBar.interactable = interactable;
        }
    }
}
