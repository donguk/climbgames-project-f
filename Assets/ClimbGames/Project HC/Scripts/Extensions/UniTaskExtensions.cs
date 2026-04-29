using System.Threading;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System;

public static class UniTaskExtensions
{
    //public static UniTask ToUniTask(this Tween tween, CancellationToken cancellationToken = default)
    //{
    //    var tcs = new UniTaskCompletionSource();
    //
    //    tween.OnComplete(() => tcs.TrySetResult())
    //         .OnKill(() => tcs.TrySetCanceled());
    //
    //    cancellationToken.Register(() =>
    //    {
    //        if (tween.IsActive() && tween.IsPlaying())
    //        {
    //            tween.Kill();
    //        }
    //    });
    //
    //    return tcs.Task;
    //}

    public static UniTask<T> ToUniTask<T>(this Observable<T> source, CancellationToken cancellationToken = default)
    {
        var tcs = new UniTaskCompletionSource<T>();
        IDisposable subscription = null;

        // 토큰 취소 시 동작
        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled(cancellationToken);
                subscription?.Dispose();
            });
        }

        subscription = source.Subscribe(
            value =>
            {
                tcs.TrySetResult(value);
                subscription?.Dispose();
            },
            ex =>
            {
                tcs.TrySetException(ex);
                subscription?.Dispose();
            },
            onCompleted: result =>
            {
                // 옵저버블이 아무 값 없이 끝난 경우
                tcs.TrySetCanceled();
                subscription?.Dispose();
            });

        tcs.Task.GetAwaiter().OnCompleted(() => subscription?.Dispose());
        return tcs.Task;
        //return await source.FirstAsync().AsUniTask();
    }

    //public static UniTask ToUniTask(this AnimationSequencerController sequencerController, CancellationToken cancellationToken = default)
    //{
    //    var tcs = new UniTaskCompletionSource();
    //
    //    if (sequencerController.PlayingSequence == null || sequencerController.IsPlaying == false)
    //    {
    //        tcs.TrySetResult();
    //        return tcs.Task;
    //    }
    //
    //    sequencerController.PlayingSequence.OnComplete(() => tcs.TrySetResult())
    //                                        .OnKill(() => tcs.TrySetCanceled());
    //
    //    if (cancellationToken.CanBeCanceled)
    //    {
    //        cancellationToken.Register(() =>
    //        {
    //            tcs.TrySetCanceled(cancellationToken);
    //            sequencerController.Kill();
    //        });
    //    }
    //
    //    return tcs.Task;
    //}
}
