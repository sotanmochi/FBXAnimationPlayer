using System;
using UnityEngine;

namespace FbxAnimationPlayer.Samples
{
    public sealed class CameraBackgroundColorController
    {
        private Camera _camera;

        public event Action<Color> ColorChanged;

        public Color CurrentColor => _camera != null ? _camera.backgroundColor : Color.black;

        public void SetCamera(Camera camera)
        {
            _camera = camera;
        }

        public void SetColor(Color color)
        {
            if (_camera == null) return;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = color;
            ColorChanged?.Invoke(color);
        }

        public void SetColorFromRGB(float r, float g, float b)
        {
            SetColor(new Color(r, g, b, 1f));
        }
    }
}
