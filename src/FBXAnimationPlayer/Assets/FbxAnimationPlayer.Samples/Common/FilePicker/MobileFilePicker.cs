using System;
using System.Linq;

namespace FbxAnimationPlayer.Samples
{
    public sealed class MobileFilePicker : IFilePicker
    {
        public void PickFile(string title, string[] extensions, Action<string> onPicked)
        {
            // NativeFilePicker はファイルを一時領域にコピーしてパスを返す
            // ConvertExtensionToFileType により iOS は UTI、Android は MIME タイプに自動変換される
            var fileTypes = extensions
                .Select(ext => NativeFilePicker.ConvertExtensionToFileType(ext))
                .ToArray();

            NativeFilePicker.PickFile(path => onPicked(path), fileTypes);
        }
    }
}
