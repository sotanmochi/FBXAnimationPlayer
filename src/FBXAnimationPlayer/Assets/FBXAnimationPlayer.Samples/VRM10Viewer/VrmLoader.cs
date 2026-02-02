using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using SFB;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UniVRM10;

namespace FbxAnimationPlayer.Samples
{
    public class VrmLoader : MonoBehaviour
    {
        [SerializeField] private Button _loadButton;

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public event Action<GameObject> ModelLoaded;

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
            var paths = StandaloneFileBrowser.OpenFilePanel("Open VRM File", "", "vrm", false);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                LoadVrmModel(paths[0]);
            }
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
                var webRequest = UnityWebRequest.Get(path);
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

            var instance = await Vrm10.LoadBytesAsync(bytes, canLoadVrm0X: true, showMeshes: true, ct: cancellationToken);
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
