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

    [Serializable]
    public struct TimeUpdatePayload
    {
        public float current;
        public float duration;
    }

    [Serializable]
    public struct BackgroundColorPayload
    {
        public float r;
        public float g;
        public float b;
    }

    [Serializable]
    public struct FoVPayload
    {
        public float fov;
    }
}
