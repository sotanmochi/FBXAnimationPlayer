using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using UniVRM10;

namespace FbxAnimationPlayer.Samples
{
    public sealed class VrmLoader : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
#if !UNITY_WEBGL
        private readonly IFilePicker _filePicker = FilePickerFactory.Create();
#endif

        private Button _loadButton;

        public event Action<GameObject> ModelLoaded;

        public void Setup(VisualElement root)
        {
            _loadButton = root.Q<Button>("load-vrm-button");
            _loadButton.clicked += OpenFilePicker;
        }

        public void Dispose()
        {
            if (_loadButton != null) _loadButton.clicked -= OpenFilePicker;
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        private void OpenFilePicker()
        {
#if !UNITY_WEBGL
            _filePicker.PickFile("Open VRM File", new[] { "vrm" }, async path =>
            {
                if (path == null) return;
                await UniTask.SwitchToMainThread();
                LoadVrmModel(path);
            });
#endif
        }

        public void LoadVrmModel(string path)
        {
            LoadVrmModelAsync(path, _cancellationTokenSource.Token).Forget();
        }

        public async UniTaskVoid LoadVrmModelAsync(string path, CancellationToken cancellationToken)
        {
            byte[] bytes = null;

            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                using var webRequest = UnityWebRequest.Get(path);
                await webRequest.SendWebRequest();
                bytes = webRequest.downloadHandler.data;
            }
            else
            {
                bytes = await File.ReadAllBytesAsync(path, cancellationToken);
            }

            if (bytes == null)
            {
                Debug.Log("<color=orange>Failed to load VRM file</color>");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            var instance = await Vrm10.LoadBytesAsync(bytes, canLoadVrm0X: true, showMeshes: true, ct: cancellationToken,
                awaitCaller: new UniGLTF.RuntimeOnlyNoThreadAwaitCaller());
#else
            var instance = await Vrm10.LoadBytesAsync(bytes, canLoadVrm0X: true, showMeshes: true, ct: cancellationToken);
#endif
            if (instance == null)
            {
                Debug.Log("<color=orange>Failed to parse VRM model</color>");
                return;
            }

            await UniTask.DelayFrame(1, cancellationToken: cancellationToken); // NOTE: Wait for ControlRig to be applied.

            instance.transform.rotation = Quaternion.Euler(0, 180, 0);
            ModelLoaded?.Invoke(instance.gameObject);
        }
    }
}
