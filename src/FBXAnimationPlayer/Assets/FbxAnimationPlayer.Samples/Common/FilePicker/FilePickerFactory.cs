namespace FbxAnimationPlayer.Samples
{
    public static class FilePickerFactory
    {
        public static IFilePicker Create()
        {
#if UNITY_EDITOR
            return new StandaloneFilePicker();
#elif UNITY_IOS || UNITY_ANDROID
            return new MobileFilePicker();
#elif UNITY_WEBGL
            throw new System.NotSupportedException("File picker is not supported for WebGL.");
#else
            return new StandaloneFilePicker();
#endif
        }
    }
}
