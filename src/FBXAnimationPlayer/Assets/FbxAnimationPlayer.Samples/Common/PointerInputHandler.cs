using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace FbxAnimationPlayer.Samples
{
    public sealed class PointerInputHandler : MonoBehaviour
    {
        [Header("UI Toolkit")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private string _rootContainerClassName = "root-container";

        [Header("Mouse Sensitivity")]
        [SerializeField] private float _primaryDragSensitivity = 3f;
        [SerializeField] private float _secondaryDragSensitivity = 0.05f;
        [SerializeField] private float _scrollSensitivity = 0.1f;

        [Header("Touch Sensitivity")]
        [SerializeField] private float _touchDragSensitivity = 0.2f;
        [SerializeField] private float _touchSecondaryDragSensitivity = 0.001f;
        [SerializeField] private float _pinchSensitivity = 0.007f;

        private VisualElement _rootContainer;
        private Vector2 _lastTouchMid;
        private float _lastPinchDist;
        private bool _isTwoFingerActive;

        public Vector2 PrimaryDragDelta { get; private set; }
        public Vector2 SecondaryDragDelta { get; private set; }
        public float ZoomDelta { get; private set; }
        public bool IsPointerOverUI { get; private set; }

        void Update()
        {
            PrimaryDragDelta = Vector2.zero;
            SecondaryDragDelta = Vector2.zero;
            ZoomDelta = 0f;
            IsPointerOverUI = CheckPointerOverUI();

            if (Input.touchCount >= 2)
            {
                ProcessTwoFingerInput();
            }
            else if (Input.touchCount == 1)
            {
                _isTwoFingerActive = false;
                ProcessOneFingerInput();
            }
            else
            {
                _isTwoFingerActive = false;
                ProcessMouseInput();
            }
        }

        private void ProcessMouseInput()
        {
            var dx = Input.GetAxis("Mouse X");
            var dy = Input.GetAxis("Mouse Y");
            var scroll = Input.mouseScrollDelta.y;

            if (Input.GetMouseButton(0))
            {
                PrimaryDragDelta = new Vector2(dx, dy) * _primaryDragSensitivity;
            }

            if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                SecondaryDragDelta = new Vector2(dx, dy) * _secondaryDragSensitivity;
            }

            if (Mathf.Abs(scroll) > 0.01f)
            {
                ZoomDelta = scroll * _scrollSensitivity;
            }
        }

        private void ProcessOneFingerInput()
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                PrimaryDragDelta = touch.deltaPosition * _touchDragSensitivity;
            }
        }

        private void ProcessTwoFingerInput()
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);

            var mid = (t0.position + t1.position) * 0.5f;
            var dist = Vector2.Distance(t0.position, t1.position);

            if (!_isTwoFingerActive || t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            {
                _lastTouchMid = mid;
                _lastPinchDist = dist;
                _isTwoFingerActive = true;
                return;
            }

            // Pan
            var midDelta = mid - _lastTouchMid;
            SecondaryDragDelta = midDelta * _touchSecondaryDragSensitivity;

            // Pinch zoom
            var pinchDelta = dist - _lastPinchDist;
            ZoomDelta = pinchDelta * _pinchSensitivity;

            _lastTouchMid = mid;
            _lastPinchDist = dist;
        }

        private bool CheckPointerOverUI()
        {
            if (_uiDocument != null)
            {
                // Check if pointer is over any UI element in the UIDocument

                var root = _uiDocument.rootVisualElement;
                if (root?.panel == null) return false;

                if (_rootContainer == null)
                {
                    _rootContainer = root.Q<VisualElement>(className: _rootContainerClassName);
                }

                // Get the mouse position in screen pixel coordinates (origin is bottom-left).
                var screenPos = Input.touchCount > 0
                    ? Input.GetTouch(0).position
                    : (Vector2)Input.mousePosition;

                // Invert the Y-axis to match UI Toolkit's top-left origin.
                screenPos.y = Screen.height - screenPos.y;

                // Use the panel's utility function to convert from screen space to panel space.
                var panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, screenPos);

                var picked = root.panel.Pick(panelPos);
                if (picked == null) return false;
                if (picked == root) return false;
                if (picked == _rootContainer) return false;

                return true;
            }
            else
            {
                // Check if pointer is over any UI element using EventSystem
                return EventSystem.current != null &&
                        EventSystem.current.IsPointerOverGameObject();
            }
        }
    }
}
