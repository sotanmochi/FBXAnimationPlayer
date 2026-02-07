#if FBXANIMPLAYER_R3_SUPPORT
using R3;
#elif FBXANIMPLAYER_UNIRX_SUPPORT
using System;
using UniRx;
#endif

namespace FbxAnimationPlayer
{
    public static class FbxAnimationControllerExtensions
    {

#if FBXANIMPLAYER_R3_SUPPORT

        public static Observable<AnimationPlayState> OnStateChangedAsObservable(this FbxAnimationController controller)
        {
            return Observable.FromEvent<AnimationPlayState>(
                h => controller.StateChanged += h,
                h => controller.StateChanged -= h);
        }

        public static Observable<float> OnTimeUpdatedAsObservable(this FbxAnimationController controller)
        {
            return Observable.FromEvent<float>(
                h => controller.TimeUpdated += h,
                h => controller.TimeUpdated -= h);
        }

        public static Observable<Unit> OnClipFinishedAsObservable(this FbxAnimationController controller)
        {
            return Observable.FromEvent(
                h => controller.ClipFinished += h,
                h => controller.ClipFinished -= h);
        }

#elif FBXANIMPLAYER_UNIRX_SUPPORT

        public static IObservable<AnimationPlayState> OnStateChangedAsObservable(this FbxAnimationController controller)
        {
            return Observable.FromEvent<AnimationPlayState>(
                h => controller.StateChanged += h,
                h => controller.StateChanged -= h);
        }

        public static IObservable<float> OnTimeUpdatedAsObservable(this FbxAnimationController controller)
        {
            return Observable.FromEvent<float>(
                h => controller.TimeUpdated += h,
                h => controller.TimeUpdated -= h);
        }

        public static IObservable<Unit> OnClipFinishedAsObservable(this FbxAnimationController controller)
        {
            return Observable.FromEvent(
                h => controller.ClipFinished += h,
                h => controller.ClipFinished -= h);
        }

#endif

    }
}
