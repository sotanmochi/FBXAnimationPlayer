using System.Collections.Generic;
using UnityEngine;

namespace FbxAnimationPlayer
{
    public sealed class TPoseDebugVisualizer : MonoBehaviour
    {
        [Header("Offset")]
        [SerializeField] private Vector3 _positionOffset = Vector3.zero;
        [SerializeField] private Vector3 _rotationOffset = new Vector3(0, 180f, 0f);

        [Header("Skeleton")]
        [SerializeField] private Color _boneColor = Color.green;
        [SerializeField] private Color _jointColor = Color.yellow;
        [SerializeField] private float _jointRadius = 0.015f;

        [Header("Local Axes")]
        [SerializeField] private float _axisLength = 0.05f;

        private Material _lineMaterial;
        private Mesh _jointMesh;

        // Display toggles (controlled by UI)
        public bool ShowAxes { get; set; } = true;
        public bool ShowFbxSkeleton { get; set; }

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

        // Current snapshot to render debug info for
        private TPoseStepSnapshot _currentSnapshot;
        private Transform _fbxSkeletonRoot;

        public void SetSnapshot(TPoseStepSnapshot snapshot, TPoseStepSnapshot previousSnapshot)
        {
            _currentSnapshot = snapshot;
        }

        public void SetFbxSkeletonRoot(Transform fbxRoot)
        {
            _fbxSkeletonRoot = fbxRoot;
        }

        void Awake()
        {
            CreateLineMaterial();
            CreateJointMesh();
        }

        void OnDestroy()
        {
            if (_lineMaterial != null) Destroy(_lineMaterial);
            if (_jointMesh != null) Destroy(_jointMesh);
        }

        private void CreateLineMaterial()
        {
            var shader = Shader.Find("Hidden/Internal-Colored");
            _lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
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

        public Vector3 GetOffsetPosition(Vector3 worldPos)
        {
            var pivot = transform.position;
            var rotated = Quaternion.Euler(_rotationOffset) * (worldPos - pivot) + pivot;
            return rotated + _positionOffset;
        }

        private void OnRenderObject()
        {
            if (!Application.isPlaying || _lineMaterial == null) return;

            // FBX original skeleton
            if (ShowFbxSkeleton && _fbxSkeletonRoot != null)
            {
                DrawFbxSkeleton();
            }

            // Main skeleton bones
            _lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);
            GL.Color(_boneColor);
            DrawBoneLines(transform);
            GL.End();
            GL.PopMatrix();

            DrawJointSpheres(transform, _jointColor);

            // Local axes
            if (ShowAxes)
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
                GL.Vertex(GetOffsetPosition(parent.position));
                GL.Vertex(GetOffsetPosition(child.position));
                DrawBoneLines(child);
            }
        }

        private void DrawLocalAxes(Transform node)
        {
            var origin = GetOffsetPosition(node.position);
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

        private void DrawJointSpheres(Transform parent, Color color)
        {
            if (_jointMesh == null || _lineMaterial == null) return;

            GL.Color(color);
            var matrix = Matrix4x4.TRS(
                GetOffsetPosition(parent.position),
                Quaternion.Euler(_rotationOffset),
                Vector3.one * _jointRadius * 2f);
            _lineMaterial.SetPass(0);
            Graphics.DrawMeshNow(_jointMesh, matrix);

            foreach (Transform child in parent)
            {
                DrawJointSpheres(child, color);
            }
        }

        private void DrawFbxSkeleton()
        {
            if (_fbxSkeletonRoot == null) return;

            var fbxOffset = _positionOffset + Vector3.right * 1.5f;

            _lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);
            GL.Color(new Color(1f, 0.5f, 0f, 0.8f)); // orange
            DrawFbxBoneLines(_fbxSkeletonRoot, fbxOffset);
            GL.End();
            GL.PopMatrix();
        }

        private void DrawFbxBoneLines(Transform parent, Vector3 offset)
        {
            foreach (Transform child in parent)
            {
                var parentPos = Quaternion.Euler(_rotationOffset) * (parent.position - transform.position)
                    + transform.position + offset;
                var childPos = Quaternion.Euler(_rotationOffset) * (child.position - transform.position)
                    + transform.position + offset;
                GL.Vertex(parentPos);
                GL.Vertex(childPos);
                DrawFbxBoneLines(child, offset);
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
            Gizmos.DrawSphere(GetOffsetPosition(parent.position), _jointRadius);

            foreach (Transform child in parent)
            {
                Gizmos.color = _boneColor;
                Gizmos.DrawLine(GetOffsetPosition(parent.position), GetOffsetPosition(child.position));
                DrawGizmosHierarchy(child);
            }
        }
#endif
    }
}
