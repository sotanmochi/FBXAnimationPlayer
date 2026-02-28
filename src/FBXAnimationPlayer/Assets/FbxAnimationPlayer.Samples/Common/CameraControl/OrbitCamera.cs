using UnityEngine;

namespace FbxAnimationPlayer.Samples
{
    public sealed class OrbitCamera : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Vector3 _lookAtTarget = new(0f, 1.2f, 0f);
        [SerializeField] private float _distance = 3f;
        [SerializeField] private float _azimuth = 180f;
        [SerializeField] private float _elevation = 0f;

        [Header("Limits")]
        [SerializeField] private float _minDistance = 0.5f;
        [SerializeField] private float _maxDistance = 20f;
        [SerializeField] private float _minElevation = -80f;
        [SerializeField] private float _maxElevation = 80f;

        [Header("Field of View")]
        [SerializeField] private float _fieldOfView = 60f;
        [SerializeField] private float _minFieldOfView = 10f;
        [SerializeField] private float _maxFieldOfView = 100f;

        private Vector3 _defaultLookAtTarget;
        private float _defaultDistance;
        private float _defaultAzimuth;
        private float _defaultElevation;
        private float _defaultFieldOfView;

        public Vector3 LookAtTarget
        {
            get => _lookAtTarget;
            set { _lookAtTarget = value; ApplyOrbit(); }
        }

        public float Distance
        {
            get => _distance;
            set { _distance = Mathf.Clamp(value, _minDistance, _maxDistance); ApplyOrbit(); }
        }

        public float Azimuth
        {
            get => _azimuth;
            set { _azimuth = value; ApplyOrbit(); }
        }

        public float Elevation
        {
            get => _elevation;
            set { _elevation = Mathf.Clamp(value, _minElevation, _maxElevation); ApplyOrbit(); }
        }

        public float FieldOfView
        {
            get => _fieldOfView;
            set
            {
                _fieldOfView = Mathf.Clamp(value, _minFieldOfView, _maxFieldOfView);
                ApplyFieldOfView();
            }
        }

        void Awake()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            _defaultLookAtTarget = _lookAtTarget;
            _defaultDistance = _distance;
            _defaultAzimuth = _azimuth;
            _defaultElevation = _elevation;
            _defaultFieldOfView = _fieldOfView;

            ApplyOrbit();
            ApplyFieldOfView();
        }

        public void SetOrbit(float azimuth, float elevation, float distance, Vector3 target)
        {
            _azimuth = azimuth;
            _elevation = Mathf.Clamp(elevation, _minElevation, _maxElevation);
            _distance = Mathf.Clamp(distance, _minDistance, _maxDistance);
            _lookAtTarget = target;
            ApplyOrbit();
        }

        public void ResetToDefault()
        {
            SetOrbit(_defaultAzimuth, _defaultElevation, _defaultDistance, _defaultLookAtTarget);
            FieldOfView = _defaultFieldOfView;
        }

        public void Rotate(float deltaAzimuth, float deltaElevation)
        {
            _azimuth += deltaAzimuth;
            _elevation = Mathf.Clamp(_elevation + deltaElevation, _minElevation, _maxElevation);
            ApplyOrbit();
        }

        public void Pan(float dx, float dy)
        {
            var right = _camera.transform.right;
            var up = _camera.transform.up;
            _lookAtTarget -= (right * dx + up * dy) * _distance;
            ApplyOrbit();
        }

        public void Zoom(float delta)
        {
            _distance -= delta * _distance;
            _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
            ApplyOrbit();
        }

        private void ApplyOrbit()
        {
            var radElevation = _elevation * Mathf.Deg2Rad;
            var radAzimuth = _azimuth * Mathf.Deg2Rad;

            var offset = new Vector3(
                _distance * Mathf.Cos(radElevation) * Mathf.Sin(radAzimuth),
                _distance * Mathf.Sin(radElevation),
                _distance * Mathf.Cos(radElevation) * Mathf.Cos(radAzimuth)
            );

            _camera.transform.position = _lookAtTarget + offset;
            _camera.transform.LookAt(_lookAtTarget);
        }

        private void ApplyFieldOfView()
        {
            _camera.fieldOfView = _fieldOfView;
        }
    }
}
