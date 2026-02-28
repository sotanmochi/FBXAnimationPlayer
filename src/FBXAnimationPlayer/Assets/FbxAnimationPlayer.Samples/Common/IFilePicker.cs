using System;

namespace FbxAnimationPlayer.Samples
{
    public interface IFilePicker
    {
        void PickFile(string title, string[] extensions, Action<string> onPicked);
    }
}
