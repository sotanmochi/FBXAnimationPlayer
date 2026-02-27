using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace FbxAnimationPlayer.Samples
{
    public sealed class FbxLoader : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly IFilePicker _filePicker;

        private Button _loadButton;

        public event Action<ImportResult> FbxAnimationLoaded;

        public FbxLoader(IFilePickerFactory filePickerFactory)
        {
            _filePicker = filePickerFactory?.Create();
        }

        public void Setup(VisualElement root)
        {
            _loadButton = root.Q<Button>("load-fbx-button");
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
            _filePicker.PickFile("Open FBX File", new[] { "fbx" }, async path =>
            {
                if (path == null) return;
                await UniTask.SwitchToMainThread();
                LoadFbxAnimation(path);
            });
#endif
        }

        public void LoadFbxAnimation(string path)
        {
            LoadFbxAnimationAsync(path, _cancellationTokenSource.Token).Forget();
        }

        public async UniTaskVoid LoadFbxAnimationAsync(string path, CancellationToken cancellationToken)
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
                Debug.Log("<color=orange>Failed to load FBX file</color>");
                return;
            }

            var importResult = await FbxAnimationImporter.LoadAsync(new MemoryStream(bytes), cancellationToken);
            if (!importResult.IsSuccess)
            {
                Debug.Log($"<color=orange>Failed to import FBX animation: {importResult.ErrorMessage}</color>");
                return;
            }
            FbxAnimationLoaded?.Invoke(importResult);
        }
    }
}
