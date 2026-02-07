using System;
using System.Collections.Generic;
using UnityEngine;

namespace FbxAnimationPlayer
{
    public sealed class FbxAnimationController : MonoBehaviour, IDisposable
    {
        [ReadOnly, SerializeField]
        private AnimationPlayState _state = AnimationPlayState.Stopped;

        [ReadOnly, SerializeField]
        private float _currentTime;

        [ReadOnly, SerializeField]
        private int _currentClipIndex;

        [ReadOnly, NonReorderable, SerializeField]
        private List<AnimationClip> _clips;

        [ReadOnly, SerializeField]
        private GameObject _fbxRootObject;

        public AnimationPlayState State => _state;
        public float Duration => HasClips ? _clips[_currentClipIndex].length : 0f;
        public float CurrentTime => _currentTime;
        public float NormalizedTime
        {
            get
            {
                var duration = Duration;
                return duration > 0f ? Mathf.Clamp01(_currentTime / duration) : 0f;
            }
        }
        public bool HasClips => _clips != null && _clips.Count > 0;
        public int ClipCount => _clips?.Count ?? 0;
        public int CurrentClipIndex => _currentClipIndex;

        public float Speed { get; set; } = 1.0f;
        public bool IsLooping { get; set; } = true;
        public bool UseManualUpdate { get; set; } = false;

        public event Action<AnimationPlayState> StateChanged;
        public event Action<float> TimeUpdated;
        public event Action ClipFinished;

        void Update()
        {
            if (UseManualUpdate) return;
            UpdateInternal(Time.deltaTime);
        }

        void OnDestroy()
        {
            if (_clips == null) return;

            foreach (var clip in _clips)
            {
                UnityEngine.Object.Destroy(clip);
            }
            _clips.Clear();
        }

        public void Dispose()
        {
            if (this != null && this.gameObject != null)
            {
                UnityEngine.Object.Destroy(this.gameObject);
            }
        }

        public void Setup(GameObject fbxRootObject, List<AnimationClip> clips)
        {
            _fbxRootObject = fbxRootObject;
            _clips = clips;
            _currentClipIndex = 0;
            _currentTime = 0f;
            SetState(AnimationPlayState.Stopped);
        }

        public void Play()
        {
            if (!HasClips) return;

            if (_state == AnimationPlayState.Paused)
            {
                SetState(AnimationPlayState.Playing);
                return;
            }

            var isTimeUpdated = _currentTime != 0f;

            _currentTime = 0f;
            EvaluateCurrentFrame();

            SetState(AnimationPlayState.Playing);
            if (isTimeUpdated)
            {
                TimeUpdated?.Invoke(_currentTime);
            }
        }

        public void Pause()
        {
            if (_state != AnimationPlayState.Playing) return;
            SetState(AnimationPlayState.Paused);
        }

        public void Stop()
        {
            _currentTime = 0f;
            EvaluateCurrentFrame();
            SetState(AnimationPlayState.Stopped);
            TimeUpdated?.Invoke(_currentTime);
        }

        public void Seek(float timeSeconds)
        {
            if (!HasClips) return;

            _currentTime = Mathf.Clamp(timeSeconds, 0f, _clips[_currentClipIndex].length);
            EvaluateCurrentFrame();

            if (_state == AnimationPlayState.Stopped)
            {
                SetState(AnimationPlayState.Paused);
            }
            TimeUpdated?.Invoke(_currentTime);
        }

        public void SeekNormalized(float normalizedTime)
        {
            if (!HasClips) return;

            _currentTime = Mathf.Clamp01(normalizedTime) * _clips[_currentClipIndex].length;
            EvaluateCurrentFrame();

            if (_state == AnimationPlayState.Stopped)
            {
                SetState(AnimationPlayState.Paused);
            }
            TimeUpdated?.Invoke(_currentTime);
        }

        public void Update(float deltaTime)
        {
            if (!UseManualUpdate) return;
            UpdateInternal(deltaTime);
        }

        private void UpdateInternal(float deltaTime)
        {
            if (_state != AnimationPlayState.Playing) return;
            if (!HasClips || Duration <= 0f) return;

            _currentTime += deltaTime * Speed;

            var clip = _clips[_currentClipIndex];

            if ((Speed >= 0f && _currentTime >= clip.length) ||
                (Speed < 0f && _currentTime <= 0f))
            {
                if (IsLooping)
                {
                    if (Speed >= 0f)
                    {
                        _currentTime %= clip.length;
                    }
                    else
                    {
                        _currentTime = clip.length + (_currentTime % clip.length);
                    }
                }
                else
                {
                    _currentTime = Speed >= 0f ? clip.length : 0f;
                    EvaluateCurrentFrame();

                    SetState(AnimationPlayState.Stopped);
                    TimeUpdated?.Invoke(_currentTime);
                    ClipFinished?.Invoke();

                    return;
                }
            }

            EvaluateCurrentFrame();
            TimeUpdated?.Invoke(_currentTime);
        }

        private void EvaluateCurrentFrame()
        {
            if (!HasClips || _fbxRootObject == null) return;
            _clips[_currentClipIndex].SampleAnimation(_fbxRootObject, _currentTime);
        }

        private void SetState(AnimationPlayState newState)
        {
            if (_state == newState) return;
            _state = newState;
            StateChanged?.Invoke(_state);
        }
    }
}
