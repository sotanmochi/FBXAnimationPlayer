using UnityEngine;

namespace FbxAnimationPlayer
{
    public sealed class SkeletonVisualizer : MonoBehaviour
    {
        [Header("Offset")]
        [SerializeField] private Vector3 _positionOffset = Vector3.zero;
        [SerializeField] private Vector3 _rotationOffset = new Vector3(0, 180f, 0f);

        [Header("Colors")]
        [SerializeField] private Color _boneColor = Color.green;
        [SerializeField] private Color _jointColor = Color.yellow;

        [Header("Sizes")]
        [SerializeField] private float _jointRadius = 0.015f;

        [Header("Local Axes")]
        [SerializeField] private bool _showAxes = true;
        [SerializeField] private float _axisLength = 0.05f;

        private Material _lineMaterial;
        private Mesh _jointMesh;

        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        public bool ShowAxes
        {
            get => _showAxes;
            set => _showAxes = value;
        }

        public Vector3 PositionOffset
        {
            get => _positionOffset;
            set => _positionOffset = value;
        }

        public Vector3 RotationOffset
        {
            get => _rotationOffset;
            set => _rotationOffset = value;
        }

        void Awake()
        {
            CreateLineMaterial();
            CreateJointMesh();
        }

        void OnDestroy()
        {
            if (_lineMaterial != null)
            {
                Destroy(_lineMaterial);
            }
            if (_jointMesh != null)
            {
                Destroy(_jointMesh);
            }
        }

        private void CreateLineMaterial()
        {
            var shader = Shader.Find("Hidden/Internal-Colored");
            _lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _lineMaterial.SetInt("_ZWrite", 0);
        }

        private void CreateJointMesh()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _jointMesh = go.GetComponent<MeshFilter>().mesh;
            Destroy(go);
        }

        private Vector3 ApplyOffset(Vector3 worldPos)
        {
            var pivot = transform.position;
            var rotated = Quaternion.Euler(_rotationOffset) * (worldPos - pivot) + pivot;
            return rotated + _positionOffset;
        }

        private void OnRenderObject()
        {
            if (!Application.isPlaying) return;
            if (_lineMaterial == null) return;

            // Draw bone lines
            _lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);
            GL.Color(_boneColor);
            DrawBoneLines(transform);
            GL.End();
            GL.PopMatrix();

            DrawJointSpheres(transform);

            // Draw local axes
            if (_showAxes)
            {
                _lineMaterial.SetPass(0);
                GL.PushMatrix();
                GL.MultMatrix(Matrix4x4.identity);
                GL.Begin(GL.LINES);
                DrawLocalAxes(transform);
                GL.End();
                GL.PopMatrix();
            }
        }

        private void DrawBoneLines(Transform parent)
        {
            foreach (Transform child in parent)
            {
                GL.Vertex(ApplyOffset(parent.position));
                GL.Vertex(ApplyOffset(child.position));
                DrawBoneLines(child);
            }
        }

        private void DrawLocalAxes(Transform node)
        {
            var origin = ApplyOffset(node.position);
            var rot = Quaternion.Euler(_rotationOffset) * node.rotation;

            GL.Color(Color.red);
            GL.Vertex(origin);
            GL.Vertex(origin + rot * Vector3.right * _axisLength);

            GL.Color(Color.green);
            GL.Vertex(origin);
            GL.Vertex(origin + rot * Vector3.up * _axisLength);

            GL.Color(Color.blue);
            GL.Vertex(origin);
            GL.Vertex(origin + rot * Vector3.forward * _axisLength);

            foreach (Transform child in node)
            {
                DrawLocalAxes(child);
            }
        }

        private void DrawJointSpheres(Transform parent)
        {
            if (_jointMesh == null || _lineMaterial == null) return;

            var matrix = Matrix4x4.TRS(ApplyOffset(parent.position), Quaternion.Euler(_rotationOffset), Vector3.one * _jointRadius * 2f);
            _lineMaterial.SetPass(0);
            Graphics.DrawMeshNow(_jointMesh, matrix);

            foreach (Transform child in parent)
            {
                DrawJointSpheres(child);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            DrawGizmosHierarchy(transform);
        }

        private void DrawGizmosHierarchy(Transform parent)
        {
            Gizmos.color = _jointColor;
            Gizmos.DrawSphere(ApplyOffset(parent.position), _jointRadius);

            if (_showAxes)
            {
                var origin = ApplyOffset(parent.position);
                var rot = Quaternion.Euler(_rotationOffset) * parent.rotation;

                Gizmos.color = Color.red;
                Gizmos.DrawLine(origin, origin + rot * Vector3.right * _axisLength);

                Gizmos.color = Color.green;
                Gizmos.DrawLine(origin, origin + rot * Vector3.up * _axisLength);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(origin, origin + rot * Vector3.forward * _axisLength);
            }

            foreach (Transform child in parent)
            {
                Gizmos.color = _boneColor;
                Gizmos.DrawLine(ApplyOffset(parent.position), ApplyOffset(child.position));
                DrawGizmosHierarchy(child);
            }
        }
#endif
    }
}