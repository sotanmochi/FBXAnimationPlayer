using System;
using UnityEngine.UIElements;

namespace FbxAnimationPlayer.Samples
{
    public sealed class AnimationControlPanel : IDisposable
    {
        private Button _playPauseButton;
        private Button _stopButton;
        private Toggle _loopToggle;
        private Slider _seekBar;
        private Label _timeLabel;

        private FbxAnimationController _animationController;

        public void Setup(VisualElement root)
        {
            _playPauseButton = root.Q<Button>("play-pause-button");
            _stopButton      = root.Q<Button>("stop-button");
            _loopToggle      = root.Q<Toggle>("loop-toggle");
            _seekBar         = root.Q<Slider>("seek-bar");
            _timeLabel       = root.Q<Label>("time-label");

            _playPauseButton.clicked += OnPlayPauseClicked;
            _stopButton.clicked      += OnStopClicked;
            _loopToggle.RegisterValueChangedCallback(OnLoopToggleValueChanged);
            _seekBar.RegisterValueChangedCallback(OnSeekBarValueChanged);

            SetEnabled(false);
        }

        public void Dispose()
        {
            Unbind();

            // Setup() が呼ばれていない場合は全フィールドが null のため何もしない
            if (_playPauseButton == null) return;

            _playPauseButton.clicked -= OnPlayPauseClicked;
            _stopButton.clicked      -= OnStopClicked;
            _loopToggle.UnregisterValueChangedCallback(OnLoopToggleValueChanged);
            _seekBar.UnregisterValueChangedCallback(OnSeekBarValueChanged);
        }

        public void Bind(FbxAnimationController controller)
        {
            Unbind();

            _animationController = controller;
            _animationController.StateChanged += OnStateChanged;
            _animationController.TimeUpdated  += OnTimeUpdated;

            _seekBar.SetValueWithoutNotify(0f);
            UpdateTimeLabel(0f, _animationController.Duration);
            UpdatePlayPauseButton(_animationController.State);
            SetEnabled(true);
        }

        private void Unbind()
        {
            if (_animationController == null) return;

            _animationController.StateChanged -= OnStateChanged;
            _animationController.TimeUpdated  -= OnTimeUpdated;
            _animationController = null;

            SetEnabled(false);
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

        private void OnLoopToggleValueChanged(ChangeEvent<bool> evt)
        {
            if (_animationController == null) return;
            _animationController.IsLooping = evt.newValue;
        }

        private void OnSeekBarValueChanged(ChangeEvent<float> evt)
        {
            _animationController?.SeekNormalized(evt.newValue);
        }

        private void OnStateChanged(AnimationPlayState state)
        {
            UpdatePlayPauseButton(state);
        }

        private void OnTimeUpdated(float currentTime)
        {
            if (_animationController == null) return;
            _seekBar.SetValueWithoutNotify(_animationController.NormalizedTime);
            UpdateTimeLabel(currentTime, _animationController.Duration);
        }

        private void UpdatePlayPauseButton(AnimationPlayState state)
        {
            _playPauseButton.text = state == AnimationPlayState.Playing ? "Pause" : "Play";
        }

        private void UpdateTimeLabel(float currentTime, float duration)
        {
            _timeLabel.text = $"{currentTime:F2} / {duration:F2} [s]";
        }

        private void SetEnabled(bool enabled)
        {
            _playPauseButton.SetEnabled(enabled);
            _stopButton.SetEnabled(enabled);
            _seekBar.SetEnabled(enabled);
        }
    }
}
