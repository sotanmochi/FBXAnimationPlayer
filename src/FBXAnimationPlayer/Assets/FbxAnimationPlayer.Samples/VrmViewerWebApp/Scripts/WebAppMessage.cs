using System;

namespace FbxAnimationPlayer.Samples
{
    [Serializable]
    public struct Message
    {
        public string type;
        public string payload;
    }

    [Serializable]
    public struct UrlPayload
    {
        public string url;
    }

    [Serializable]
    public struct SeekPayload
    {
        public float normalizedTime;
    }

    [Serializable]
    public struct LoopPayload
    {
        public bool enabled;
    }

    [Serializable]
    public struct SpeedPayload
    {
        public float speed;
    }
}
