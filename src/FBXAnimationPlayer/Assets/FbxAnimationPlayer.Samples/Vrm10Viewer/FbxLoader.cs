using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using SFB;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace FbxAnimationPlayer.Samples
{
    public class FbxLoader : MonoBehaviour
    {
        [SerializeField] private Button _loadButton;

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public event Action<ImportResult> FbxAnimationLoaded;

        void Awake()
        {
            _loadButton.onClick.AddListener(OpenFileBrowser);
        }

        void OnDestroy()
        {
            _loadButton.onClick.RemoveAllListeners();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        private void OpenFileBrowser()
        {
            var paths = StandaloneFileBrowser.OpenFilePanel("Open FBX File", "", "fbx", false);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                LoadFbxAnimation(paths[0]);
            }
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
