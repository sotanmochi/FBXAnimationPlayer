using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FbxAnimationPlayer.Samples
{
    public sealed class SettingsPanel : IDisposable
    {
        private Button _settingsButton;
        private VisualElement _settingsPanel;
        private VisualElement _colorPreview;
        private Slider _sliderR;
        private Slider _sliderG;
        private Slider _sliderB;
        private Slider _fovSlider;
        private Button _resetCameraButton;

        private CameraBackgroundColorController _bgColor;
        private bool _suppressColorCallback;

        public event Action<float> FoVChanged;
        public event Action ResetCameraRequested;

        public void Setup(VisualElement root, CameraBackgroundColorController bgColor)
        {
            _bgColor = bgColor;

            _settingsButton = root.Q<Button>("settings-button");
            _settingsPanel = root.Q<VisualElement>("settings-panel");
            _colorPreview = root.Q<VisualElement>("color-preview");
            _sliderR = root.Q<Slider>("color-r");
            _sliderG = root.Q<Slider>("color-g");
            _sliderB = root.Q<Slider>("color-b");
            _fovSlider = root.Q<Slider>("fov-slider");
            _resetCameraButton = root.Q<Button>("reset-camera-button");

            _settingsButton.clicked += OnSettingsButtonClicked;
            _sliderR.RegisterValueChangedCallback(OnColorSliderChanged);
            _sliderG.RegisterValueChangedCallback(OnColorSliderChanged);
            _sliderB.RegisterValueChangedCallback(OnColorSliderChanged);
            _fovSlider.RegisterValueChangedCallback(OnFoVSliderChanged);
            _resetCameraButton.clicked += OnResetCameraClicked;

            var currentColor = _bgColor.CurrentColor;
            _suppressColorCallback = true;
            _sliderR.SetValueWithoutNotify(currentColor.r);
            _sliderG.SetValueWithoutNotify(currentColor.g);
            _sliderB.SetValueWithoutNotify(currentColor.b);
            _suppressColorCallback = false;
            UpdateColorPreview();
        }

        public void Dispose()
        {
            _settingsButton.clicked -= OnSettingsButtonClicked;
            _sliderR.UnregisterValueChangedCallback(OnColorSliderChanged);
            _sliderG.UnregisterValueChangedCallback(OnColorSliderChanged);
            _sliderB.UnregisterValueChangedCallback(OnColorSliderChanged);
            _fovSlider.UnregisterValueChangedCallback(OnFoVSliderChanged);
            _resetCameraButton.clicked -= OnResetCameraClicked;
        }

        private void OnSettingsButtonClicked()
        {
            var isVisible = _settingsPanel.style.display == DisplayStyle.Flex;
            _settingsPanel.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void OnColorSliderChanged(ChangeEvent<float> evt)
        {
            if (_suppressColorCallback) return;

            _bgColor.SetColorFromRGB(_sliderR.value, _sliderG.value, _sliderB.value);
            UpdateColorPreview();
        }

        private void OnResetCameraClicked()
        {
            ResetCameraRequested?.Invoke();
        }

        private void OnFoVSliderChanged(ChangeEvent<float> evt)
        {
            FoVChanged?.Invoke(evt.newValue);
        }

        private void UpdateColorPreview()
        {
            if (_colorPreview == null) return;
            var color = new Color(_sliderR.value, _sliderG.value, _sliderB.value);
            _colorPreview.style.backgroundColor = new StyleColor(color);
        }
    }
}
