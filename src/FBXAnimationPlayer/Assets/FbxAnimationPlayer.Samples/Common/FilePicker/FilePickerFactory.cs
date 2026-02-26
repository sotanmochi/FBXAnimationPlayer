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
#else
            return new StandaloneFilePicker();
#endif
        }
    }
}
