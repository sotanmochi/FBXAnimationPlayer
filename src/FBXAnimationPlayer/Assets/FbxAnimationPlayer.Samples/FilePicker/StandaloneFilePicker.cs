#if UNITY_STANDALONE || UNITY_EDITOR
using System;
using SFB;

namespace FbxAnimationPlayer.Samples
{
    public sealed class StandaloneFilePicker : IFilePicker
    {
        public void PickFile(string title, string[] extensions, Action<string> onPicked)
        {
            var filters = new[] { new ExtensionFilter(title, extensions) };
            StandaloneFileBrowser.OpenFilePanelAsync(title, "", filters, false, paths =>
            {
                onPicked(paths != null && paths.Length > 0 ? paths[0] : null);
            });
        }
    }
}
#endif
