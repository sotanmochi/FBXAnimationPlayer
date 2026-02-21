using UnityEngine;
using UnityEngine.UIElements;

namespace FbxAnimationPlayer.Samples
{
    /// <summary>
    /// UIDocument のルート VisualElement に SafeArea に応じたパディングを適用する。
    /// ノッチやホームバーのある iOS / Android デバイスに対応する。
    /// </summary>
    public sealed class SafeAreaHandler : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _root;
        private Rect _lastSafeArea;
        private bool _layoutResolved;

        void OnEnable()
        {
            _root = _uiDocument.rootVisualElement;
            _lastSafeArea = default;
            _layoutResolved = false;

            // GeometryChangedEvent はレイアウトパスが完了した後に発火する。
            // resolvedStyle.width/height が確定してから ApplySafeArea を呼ぶためにここで登録する。
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnDisable()
        {
            if (_root != null)
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void Update()
        {
            if (!_layoutResolved) return;
            if (Screen.safeArea == _lastSafeArea) return;

            // 画面回転などで SafeArea が変化したときに再適用する
            ApplySafeArea();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            _layoutResolved = true;
            if (Screen.safeArea != _lastSafeArea)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            var safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;

            // Screen.safeArea は物理ピクセル単位。
            // resolvedStyle.width/height は PanelSettings のスケールを反映したレイアウトピクセル単位。
            // スケール係数を掛けることで、どの PanelSettings 設定でも正しく動作する。
            var panelWidth  = _root.resolvedStyle.width;
            var panelHeight = _root.resolvedStyle.height;
            var scaleX = panelWidth  / Screen.width;
            var scaleY = panelHeight / Screen.height;

            // Screen.safeArea は左下原点（Unityのスクリーン座標系）。
            // UI Toolkit は左上原点なので paddingTop / paddingBottom を変換する。
            _root.style.paddingTop    = (Screen.height - safeArea.yMax) * scaleY;
            _root.style.paddingBottom = safeArea.yMin                   * scaleY;
            _root.style.paddingLeft   = safeArea.xMin                   * scaleX;
            _root.style.paddingRight  = (Screen.width - safeArea.xMax)  * scaleX;
        }
    }
}
