using UnityEngine;
using R3;
using R3.Triggers;
using System;

namespace com.pickplay.carryons
{
    public static class ObservableExtensions
    {
        public static Observable<T> StartWith<T>(this Observable<T> source, T value)
        {
            return Observable.Create<T>(observer =>
            {
                // 먼저 value를 emit
                observer.OnNext(value);

                // 원본 Observable 구독
                return source.Subscribe(observer);
            });
        }

        public static Observable<Unit> OnStateExitAsObservable(this Animator animator, string stateName)
        {
            if (animator == null) throw new ArgumentNullException(nameof(animator));
            if (string.IsNullOrEmpty(stateName)) throw new ArgumentNullException(nameof(stateName));

            return Observable.EveryUpdate()
                .Select(_ => animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
                .DistinctUntilChanged() // 변화 감지
                .Pairwise()             // 이전값, 현재값 비교
                .Where(pair => pair.Previous == true && pair.Current == false)
                .Take(1)
                .AsUnitObservable();
        }

        public static Observable<Unit> OnNormalizedTimeReachedAsObservable(this Animator animator, string stateName, float targetNormalized, int layer = 0)
        {
            if (animator == null) throw new ArgumentNullException(nameof(animator));
            if (string.IsNullOrEmpty(stateName)) throw new ArgumentNullException(nameof(stateName));

            return Observable.EveryUpdate()
                .Select(_ => animator.GetCurrentAnimatorStateInfo(layer))
                .Where(state => state.IsName(stateName))
                .Select(state => state.normalizedTime % 1f) // loop 대응
                .Where(normalized => normalized >= targetNormalized)
                .Take(1)
                .AsUnitObservable();
        }
    }
}
