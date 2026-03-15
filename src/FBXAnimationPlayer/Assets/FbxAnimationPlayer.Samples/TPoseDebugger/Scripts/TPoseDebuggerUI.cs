using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FbxAnimationPlayer.Samples
{
    [RequireComponent(typeof(UIDocument))]
    public class TPoseDebuggerUI : MonoBehaviour
    {
        [SerializeField] private TPoseDebugger _debugger;

        private UIDocument _uiDocument;

        // Navigation
        private Button _resetButton, _prevButton, _nextButton, _runAllButton;
        private Label _labelStep, _labelStepName;
        private Slider _sliderStep;
        private bool _isUpdatingSlider;

        // Step Info
        private Label _labelParent, _labelChild, _labelCurrentDir, _labelExpectedDir;
        private Label _labelDot, _labelCorrection, _labelStatus;

        // Display Options
        private Toggle _toggleFbxSkeleton, _toggleAxes;

        // File Info
        private Label _labelFilename;

        // Bone Map
        private Foldout _foldoutBoneMap;
        private VisualElement _boneMapList;

        void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            var root = _uiDocument.rootVisualElement;

            // Navigation
            _resetButton = root.Q<Button>("reset-button");
            _prevButton = root.Q<Button>("prev-button");
            _nextButton = root.Q<Button>("next-button");
            _runAllButton = root.Q<Button>("run-all-button");
            _labelStep = root.Q<Label>("label-step");
            _labelStepName = root.Q<Label>("label-step-name");
            _sliderStep = root.Q<Slider>("slider-step");

            _resetButton.clicked += OnResetClicked;
            _prevButton.clicked += OnPrevClicked;
            _nextButton.clicked += OnNextClicked;
            _runAllButton.clicked += OnRunAllClicked;
            _sliderStep.RegisterValueChangedCallback(OnSliderChanged);

            // Step Info
            _labelParent = root.Q<Label>("label-parent");
            _labelChild = root.Q<Label>("label-child");
            _labelCurrentDir = root.Q<Label>("label-current-dir");
            _labelExpectedDir = root.Q<Label>("label-expected-dir");
            _labelDot = root.Q<Label>("label-dot");
            _labelCorrection = root.Q<Label>("label-correction");
            _labelStatus = root.Q<Label>("label-status");

            // Display Options
            _toggleFbxSkeleton = root.Q<Toggle>("toggle-fbx-skeleton");
            _toggleAxes = root.Q<Toggle>("toggle-axes");

            _toggleFbxSkeleton.RegisterValueChangedCallback(e => SyncVisualizerToggles());
            _toggleAxes.RegisterValueChangedCallback(e => SyncVisualizerToggles());

            // File Info
            _labelFilename = root.Q<Label>("label-filename");

            // Bone Map
            _foldoutBoneMap = root.Q<Foldout>("foldout-bone-map");
            _boneMapList = root.Q<VisualElement>("bone-map-list");

            // Bind debugger
            if (_debugger != null)
            {
                _debugger.OnSnapshotChanged += UpdateUI;
                _debugger.OnBoneMapReady += PopulateBoneMap;
            }
        }

        void OnDisable()
        {
            if (_debugger != null)
            {
                _debugger.OnSnapshotChanged -= UpdateUI;
                _debugger.OnBoneMapReady -= PopulateBoneMap;
            }

            _resetButton.clicked -= OnResetClicked;
            _prevButton.clicked -= OnPrevClicked;
            _nextButton.clicked -= OnNextClicked;
            _runAllButton.clicked -= OnRunAllClicked;
        }

        private void OnResetClicked() => _debugger?.ResetToInitial();
        private void OnPrevClicked() => _debugger?.StepBackward();
        private void OnNextClicked() => _debugger?.StepForward();
        private void OnRunAllClicked() => _debugger?.RunAll();

        private void OnSliderChanged(ChangeEvent<float> evt)
        {
            if (_isUpdatingSlider || _debugger == null || !_debugger.IsLoaded) return;
            var targetStep = Mathf.RoundToInt(evt.newValue);
            _debugger.GoToStep(targetStep);
        }

        private void UpdateUI(TPoseStepSnapshot snapshot)
        {
            if (snapshot == null || _debugger == null) return;

            // Update navigation
            _labelStep.text = $"Step {_debugger.CurrentStep} / {_debugger.TotalSteps - 1}";
            _labelStepName.text = snapshot.StepName;

            _isUpdatingSlider = true;
            _sliderStep.lowValue = 0;
            _sliderStep.highValue = _debugger.TotalSteps - 1;
            _sliderStep.value = _debugger.CurrentStep;
            _isUpdatingSlider = false;

            // Update button states
            _prevButton.SetEnabled(_debugger.CurrentStep > 0);
            _nextButton.SetEnabled(_debugger.CurrentStep < _debugger.TotalSteps - 1);

            // Update step info
            var info = snapshot.DebugInfo;
            if (info == null)
            {
                ClearStepInfo();
                return;
            }

            // Chain step
            if (info.ParentBone.HasValue)
            {
                _labelParent.text = $"Parent: {info.ParentBone.Value}";
                _labelChild.text = $"Child: {info.ChildBone?.ToString() ?? "-"}";
                _labelCurrentDir.text = info.CurrentDirection.HasValue
                    ? $"Current Dir: {FormatVector3(info.CurrentDirection.Value)}"
                    : "Current Dir: -";
                _labelExpectedDir.text = info.ExpectedDirection.HasValue
                    ? $"Expected Dir: {FormatVector3(info.ExpectedDirection.Value)}"
                    : "Expected Dir: -";
                _labelDot.text = info.DotProduct.HasValue
                    ? $"Dot Product: {info.DotProduct.Value:F5}"
                    : "Dot Product: -";
                _labelCorrection.text = info.CorrectionRotation.HasValue
                    ? $"Correction: {FormatRotation(info.CorrectionRotation.Value)}"
                    : "Correction: -";
            }
            // Hips step
            else if (info.CurrentUp.HasValue)
            {
                _labelParent.text = "Target: Hips";
                _labelChild.text = "";
                _labelCurrentDir.text = $"Up: {FormatVector3(info.CurrentUp.Value)}";
                _labelExpectedDir.text = $"Right: {FormatVector3(info.CurrentRight ?? Vector3.right)}";
                _labelDot.text = $"Forward: {FormatVector3(info.CurrentForward ?? Vector3.forward)}";
                _labelCorrection.text = info.CorrectionRotation.HasValue
                    ? $"Correction: {FormatRotation(info.CorrectionRotation.Value)}"
                    : "Correction: -";
            }
            // Height adjustment step
            else if (info.GroundY.HasValue)
            {
                _labelParent.text = $"Ground Y: {info.GroundY.Value:F4}";
                _labelChild.text = $"Hips Height: {info.AdjustedHipsHeight?.ToString("F4") ?? "-"}";
                _labelCurrentDir.text = "";
                _labelExpectedDir.text = "";
                _labelDot.text = "";
                _labelCorrection.text = "";
            }
            else
            {
                ClearStepInfo();
            }

            // Status
            if (snapshot.WasSkipped)
            {
                _labelStatus.text = $"Status: SKIPPED - {snapshot.SkipReason}";
                _labelStatus.RemoveFromClassList("status-applied");
                _labelStatus.AddToClassList("status-skipped");
            }
            else
            {
                _labelStatus.text = "Status: Applied";
                _labelStatus.RemoveFromClassList("status-skipped");
                _labelStatus.AddToClassList("status-applied");
            }

            SyncVisualizerToggles();
        }

        private void ClearStepInfo()
        {
            _labelParent.text = "Parent: -";
            _labelChild.text = "Child: -";
            _labelCurrentDir.text = "Current Dir: -";
            _labelExpectedDir.text = "Expected Dir: -";
            _labelDot.text = "Dot Product: -";
            _labelCorrection.text = "Correction: -";
            _labelStatus.text = "Status: -";
            _labelStatus.RemoveFromClassList("status-applied");
            _labelStatus.RemoveFromClassList("status-skipped");
        }

        private void PopulateBoneMap(
            Dictionary<HumanBodyBones, Transform> fbxBoneMap,
            Dictionary<HumanBodyBones, Transform> skeletonBoneMap)
        {
            if (_labelFilename != null && _debugger != null)
            {
                _labelFilename.text = _debugger.LoadedFileName ?? "(no file loaded)";
            }

            _boneMapList.Clear();

            int mappedCount = 0;
            int totalCount = (int)HumanBodyBones.LastBone;

            for (int i = 0; i < totalCount; i++)
            {
                var bone = (HumanBodyBones)i;
                var boneName = bone.ToString();

                if (fbxBoneMap.TryGetValue(bone, out var fbxTransform))
                {
                    mappedCount++;
                    var label = new Label($"  {boneName}: \"{fbxTransform.name}\"");
                    label.AddToClassList("bone-mapped");
                    _boneMapList.Add(label);
                }
                else
                {
                    // Only show required bones as unmapped
                    if (IsRequiredBone(bone))
                    {
                        var label = new Label($"  {boneName}: (not found)");
                        label.AddToClassList("bone-unmapped");
                        _boneMapList.Add(label);
                    }
                }
            }

            _foldoutBoneMap.text = $"Bone Map [{mappedCount}/{totalCount}]";
        }

        private void SyncVisualizerToggles()
        {
            var vis = _debugger?.GetVisualizer();
            if (vis == null) return;

            vis.ShowFbxSkeleton = _toggleFbxSkeleton.value;
            vis.ShowAxes = _toggleAxes.value;
        }

        private static string FormatVector3(Vector3 v) => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";

        private static string FormatRotation(Quaternion q)
        {
            q.ToAngleAxis(out var angle, out var axis);
            return $"{angle:F1} deg, axis:{FormatVector3(axis)}";
        }

        private static bool IsRequiredBone(HumanBodyBones bone)
        {
            return bone == HumanBodyBones.Hips ||
                   bone == HumanBodyBones.Spine ||
                   bone == HumanBodyBones.Chest ||
                   bone == HumanBodyBones.Neck ||
                   bone == HumanBodyBones.Head ||
                   bone == HumanBodyBones.LeftUpperArm ||
                   bone == HumanBodyBones.LeftLowerArm ||
                   bone == HumanBodyBones.LeftHand ||
                   bone == HumanBodyBones.RightUpperArm ||
                   bone == HumanBodyBones.RightLowerArm ||
                   bone == HumanBodyBones.RightHand ||
                   bone == HumanBodyBones.LeftUpperLeg ||
                   bone == HumanBodyBones.LeftLowerLeg ||
                   bone == HumanBodyBones.LeftFoot ||
                   bone == HumanBodyBones.RightUpperLeg ||
                   bone == HumanBodyBones.RightLowerLeg ||
                   bone == HumanBodyBones.RightFoot;
        }
    }
}
