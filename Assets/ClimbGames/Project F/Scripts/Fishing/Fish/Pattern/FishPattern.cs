using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClimbGames.ProjectF
{
    // 물고기 행동 시퀀서 (입질, 파이팅을 구분하지 않음)
    // - Load: 물고기가 쓸 클립 asset 을 키당 한 번 불러옴 (Addressables 가 참조 수를 셈, 로드한 만큼 Release)
    // - Play: 생애 단계 상태가 OnEnter 에서 시퀀스(키 목록)와 끝난 뒤 규칙을 주면 FishPatternStateMachine 으로 실행
    // - 다음 클립 고르기는 peek(TryPeek*) 와 적용(Commit) 을 나눔: 여러 번 물어봐도 시퀀스 위치가 바뀌지 않게
    public class FishPattern : IDisposable
    {
        private const string AddressFormat = "PatternClip/{0}.asset";
        private const int MaxRepeat = 2;    // Random 모드에서 같은 타입 연속 최대 횟수

        private readonly Dictionary<string, FishPatternClip> clips = new Dictionary<string, FishPatternClip>();
        private readonly List<FishPatternClip> sequence = new List<FishPatternClip>();
        private FishPatternEndMode endMode;
        private int index;                  // 지금 실행 중인 시퀀스 위치
        private bool finished;
        private FishPatternStateMachine machine;

        public FishPatternClip Current => machine?.Current?.Clip;
        public FishPatternStateMachine.PatternStateBase CurrentState => machine?.Current;
        public bool IsPlaying => machine?.CurrentState != null && finished == false;
        public bool IsFinished => finished;         // Once 시퀀스를 끝까지 돌았음
        public bool HasClips => clips.Count > 0;

        // spec 이 쓰는 클립(biteClips + fightClips)을 모두 불러옴. 하나도 없으면 false
        public bool Load(FishSpec spec)
        {
            Dispose();

            if (spec == null)
                return false;

            LoadKeys(spec.biteClips);
            LoadKeys(spec.fightClips);
            return clips.Count > 0;
        }

        void LoadKeys(string[] keys)
        {
            if (keys == null)
                return;

            foreach (string key in keys)
            {
                if (string.IsNullOrEmpty(key) || clips.ContainsKey(key))
                    continue;

                // 실패하면 AssetManager 가 키와 이유를 로그로 남기고 핸들을 해제한 뒤 null 을 돌려줌
                var clip = AssetManager.LoadAsset<FishPatternClip>(string.Format(AddressFormat, key));
                if (clip != null)
                    clips.Add(key, clip);
            }
        }

        // 여러 번 불러도 안전. 클립만 해제하고 머신은 남겨서 다음 물고기에 재사용
        public void Dispose()
        {
            Stop();

            foreach (var clip in clips.Values)
                AssetManager.Release(clip);

            clips.Clear();
        }

        // 시퀀스를 다시 만들고 첫 클립부터 실행 (유효한 클립이 없으면 false)
        // T: 이 시퀀스에 들어갈 수 있는 클립 타입 (입질은 BiteClip, 파이팅은 FightClip). 다른 타입은 빼고 LogError
        public bool Play<T>(IReadOnlyList<string> keys, FishPatternEndMode mode, FishingPlayer player, HookedFish fish, FishModel model)
            where T : FishPatternClip
        {
            Stop();

            sequence.Clear();
            if (keys != null)
            {
                for (int i = 0; i < keys.Count; ++i)
                {
                    if (string.IsNullOrEmpty(keys[i]) || clips.TryGetValue(keys[i], out var clip) == false)
                        continue;

                    if (clip is T == false)
                    {
                        Debug.LogError($"[FishPattern] '{keys[i]}' 은 {typeof(T).Name} 가 아니라서 시퀀스에서 뺍니다.");
                        continue;
                    }

                    sequence.Add(clip);
                }
            }

            endMode = mode;
            index = 0;
            finished = false;

            if (sequence.Count == 0)
                return false;

            // 머신은 처음 한 번만 만들어 재사용 (같은 FishPattern 은 같은 player 가 씀)
            if (machine == null)
            {
                machine = new FishPatternStateMachine();
                machine.Setup(player, this);
            }

            machine.Play(fish, model, sequence[0]);
            return true;
        }

        // 지금 클립을 Exit 하고 정지 (상태 목록은 유지)
        public void Stop()
        {
            machine?.Stop();
        }

        // 화면을 숨길 때 (FishingPlayer.Hide): 실행만 멈춤. 다시 쓸 때는 생애 단계가 Play 로 새로 시작
        public void Pause()
        {
            machine?.Pause();
        }

        // 시퀀스에 쓸 수 있는 T 타입 클립이 하나라도 로드되어 있는지 (물고기를 만들기 전에 확인)
        public bool HasAny<T>(IReadOnlyList<string> keys) where T : FishPatternClip
        {
            if (keys == null)
                return false;

            for (int i = 0; i < keys.Count; ++i)
            {
                if (string.IsNullOrEmpty(keys[i]) == false && clips.TryGetValue(keys[i], out var clip) && clip is T)
                    return true;
            }

            return false;
        }

        // 다음 클립 (상태를 바꾸지 않음). Once 가 끝이면 false
        internal bool TryPeekNext(int repeatCount, float stamina, out FishPatternClip next, out int nextIndex)
        {
            next = null;
            nextIndex = index;

            if (sequence.Count == 0)
                return false;

            switch (endMode)
            {
                case FishPatternEndMode.Random:
                    nextIndex = PickRandom(repeatCount, stamina, null);
                    break;
                case FishPatternEndMode.Once:
                    if (index + 1 >= sequence.Count)
                        return false;
                    nextIndex = index + 1;
                    break;
                default:
                    nextIndex = (index + 1) % sequence.Count;
                    break;
            }

            next = sequence[nextIndex];
            return true;
        }

        // 지금 클립을 일찍 끝낼 때 갈 클립 (상태를 바꾸지 않음). 지금 타입과 다른 클립만, 없으면 false
        // Loop/Once: 다음 칸부터 다른 타입의 첫 클립. Random: Run 우선, 없으면 다른 타입 아무거나
        internal bool TryPeekBreakOut(float stamina, out FishPatternClip next, out int nextIndex)
        {
            next = null;
            nextIndex = index;

            if (sequence.Count == 0)
                return false;

            FishPatternState currentType = sequence[index].Type;

            if (endMode == FishPatternEndMode.Random)
            {
                int pick = PickRandom(0, stamina, FishPatternState.Run);
                if (pick < 0 || sequence[pick].Type == currentType)
                    pick = PickRandom(0, stamina, null, currentType);
                if (pick < 0 || sequence[pick].Type == currentType)
                    return false;

                nextIndex = pick;
                next = sequence[pick];
                return true;
            }

            int count = sequence.Count;
            for (int step = 1; step < count; ++step)
            {
                int i = index + step;
                if (endMode == FishPatternEndMode.Once && i >= count)
                    break;

                i %= count;
                if (sequence[i].Type != currentType)
                {
                    nextIndex = i;
                    next = sequence[i];
                    return true;
                }
            }

            return false;
        }

        // 고른 클립으로 실제로 넘어갈 때
        internal void Commit(int nextIndex)
        {
            index = Mathf.Clamp(nextIndex, 0, Mathf.Max(0, sequence.Count - 1));
        }

        // Once 시퀀스가 끝났을 때 (정지 경로는 이것 하나): 마지막 클립을 Exit 하고 정지 (입질 pull 도 여기서 정리)
        // 생애 단계 상태가 IsFinished 를 보고 다음 단계로. Advance 의 마지막 줄에서 불리므로 이후에 도는 코드 없음
        internal void Finish()
        {
            finished = true;
            machine?.Stop();
        }

        // 가중치 랜덤 (EvaluateWeight). onlyType 이 있으면 그 타입만, excludeType 이 있으면 그 타입은 빼고
        // repeatCount 가 MaxRepeat 이상이면 지금 타입은 뺌. 고를 게 없으면 -1 (연속 제한 때문이면 제한 없이 다시)
        int PickRandom(int repeatCount, float stamina, FishPatternState? onlyType, FishPatternState? excludeType = null)
        {
            FishPatternState currentType = sequence[index].Type;
            FishPatternState? blocked = repeatCount >= MaxRepeat ? currentType : (FishPatternState?)null;

            int pick = PickWeighted(stamina, onlyType, excludeType, blocked);
            if (pick < 0 && blocked.HasValue)
                pick = PickWeighted(stamina, onlyType, excludeType, null);
            if (pick < 0 && onlyType.HasValue == false && excludeType.HasValue == false)
                pick = index;   // 가중치가 모두 0 이면 지금 클립 다시

            return pick;
        }

        // 가중치 0 이하·NaN·무한대는 WeightedRandom 이 제외, 고를 게 없으면 -1 (할당 없음: 값은 state 로 넘김)
        int PickWeighted(float stamina, FishPatternState? onlyType, FishPatternState? excludeType, FishPatternState? blocked)
        {
            return WeightedRandom.Pick(sequence.Count, (sequence, stamina, onlyType, excludeType, blocked),
                static (i, s) => GetWeight(s.sequence[i], s.stamina, s.onlyType, s.excludeType, s.blocked));
        }

        static float GetWeight(FishPatternClip clip, float stamina, FishPatternState? onlyType, FishPatternState? excludeType, FishPatternState? blocked)
        {
            FishPatternState type = clip.Type;
            if (onlyType.HasValue && type != onlyType.Value)
                return 0f;
            if (excludeType.HasValue && type == excludeType.Value)
                return 0f;
            if (blocked.HasValue && type == blocked.Value)
                return 0f;

            return clip.EvaluateWeight(stamina);
        }
    }
}
