#if FBXANIMPLAYER_R3_SUPPORT
using R3;
using UnityEngine;
#elif FBXANIMPLAYER_UNIRX_SUPPORT
using System;
using UniRx;
using UnityEngine;
#endif

namespace FbxAnimationPlayer.Samples
{
    public static class WebAppMessageBusExtensions
    {

#if FBXANIMPLAYER_R3_SUPPORT

        public static Observable<Message> OnMessageReceivedAsObservable(this WebAppMessageBus bus)
        {
            return Observable.FromEvent<Message>(
                h => bus.MessageReceived += h,
                h => bus.MessageReceived -= h);
        }

        public static Observable<TPayload> OnMessageReceivedAsObservable<TPayload>(
            this WebAppMessageBus bus, string type) where TPayload : new()
        {
            return bus.OnMessageReceivedAsObservable()
                .Where(msg => msg.type == type)
                .Select(msg =>
                {
                    if (string.IsNullOrEmpty(msg.payload)) return new TPayload();
                    try   { return JsonUtility.FromJson<TPayload>(msg.payload); }
                    catch { return new TPayload(); }
                });
        }

#elif FBXANIMPLAYER_UNIRX_SUPPORT

        public static IObservable<Message> OnMessageReceivedAsObservable(this WebAppMessageBus bus)
        {
            return Observable.FromEvent<Message>(
                h => bus.MessageReceived += h,
                h => bus.MessageReceived -= h);
        }

        public static IObservable<TPayload> OnMessageReceivedAsObservable<TPayload>(
            this WebAppMessageBus bus, string type) where TPayload : new()
        {
            return bus.OnMessageReceivedAsObservable()
                .Where(msg => msg.type == type)
                .Select(msg =>
                {
                    if (string.IsNullOrEmpty(msg.payload)) return new TPayload();
                    try   { return JsonUtility.FromJson<TPayload>(msg.payload); }
                    catch { return new TPayload(); }
                });
        }

#endif

    }
}
