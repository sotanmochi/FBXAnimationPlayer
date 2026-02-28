using UnityEngine;

namespace FbxAnimationPlayer.Samples
{
    public sealed class OrbitCameraController : MonoBehaviour
    {
        [SerializeField] private OrbitCamera _camera;
        [SerializeField] private PointerInputHandler _input;

        void Update()
        {
            if (_camera == null || _input == null) return;
            if (_input.IsPointerOverUI) return;

            var primary = _input.PrimaryDragDelta;
            if (primary != Vector2.zero)
            {
                _camera.Rotate(primary.x, -primary.y);
            }

            var secondary = _input.SecondaryDragDelta;
            if (secondary != Vector2.zero)
            {
                _camera.Pan(secondary.x, secondary.y);
            }

            var zoom = _input.ZoomDelta;
            if (Mathf.Abs(zoom) > 0.001f)
            {
                _camera.Zoom(zoom);
            }
        }

        public void ResetCamera()
        {
            _camera.ResetToDefault();
        }

        public void SetFieldOfView(float fov)
        {
            _camera.FieldOfView = fov;
        }
    }
}
