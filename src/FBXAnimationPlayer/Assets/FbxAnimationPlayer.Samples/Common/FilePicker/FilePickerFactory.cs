namespace FbxAnimationPlayer.Samples
{
    public sealed class FilePickerFactory : IFilePickerFactory
    {
        public IFilePicker Create()
        {
#if UNITY_EDITOR
            return new StandaloneFilePicker();
#elif UNITY_IOS || UNITY_ANDROID
            return new MobileFilePicker();
#else
            return new StandaloneFilePicker();
#endif
        }
    }
}
