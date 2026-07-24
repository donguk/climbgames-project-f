using UnityEngine;
using ClimbGames.UI;
using UnityEngine.UI;
using TMPro;
using R3;
using R3.Triggers;
using System;

namespace ClimbGames.Project_HC
{
    public class UIFishing : UIBase
    {
        enum CastingState
        {
            None,
            Loading,
            Throw,
            Landing,
        }

        [SerializeField] private Button btn_casting;
        [SerializeField] private Slider sld_casting_gauge;
        [SerializeField] private TMP_Text txt_casting;
        [SerializeField] private Button btn_reeling;
        [SerializeField] private Toggle tgl_casting_type;
        [SerializeField] private TMP_Text txt_casting_type;
        [SerializeField] private Slider sld_tension_gauge;
        [SerializeField] private TMP_Text txt_tension_value;

        private CastingState castingState = CastingState.None;
        private int loadingDirection = 1;
        private float loadingValue;
        private FishingPlayer localPlayer;

        void OnEnable()
        {
            SetCastingState(CastingState.None);
        }

        void Start()
        {
            localPlayer = FishingPlayerManager.Instance.LocalPlayer;

            localPlayer.State.Subscribe(_ => OnChangeFishingState(_)).AddTo(this);
            btn_casting.OnClickAsObservable().Subscribe(_ => OnClickCasting()).AddTo(this);

            SetCastingType(localPlayer.CastingType);
            tgl_casting_type.OnValueChangedAsObservable().Subscribe(_ =>
            {
                CastingType type = _ ? CastingType.OverHead : CastingType.Pitching;
                localPlayer.SetCastingType(type);
                SetCastingType(type);

            }).AddTo(this);

            btn_reeling.OnPointerDownAsObservable().Subscribe(_ => FishingInput.Instance.SetReeling(true)).AddTo(this);
            btn_reeling.OnPointerUpAsObservable().Subscribe(_ => FishingInput.Instance.SetReeling(false)).AddTo(this);
        }

        void Update()
        {
            switch (castingState)
            {
                case CastingState.Loading:
                    {
                        loadingValue += Time.deltaTime * loadingDirection / (loadingDirection > 0 ? 0.8f : 1.2f);

                        if (loadingValue >= 1f)
                            loadingDirection = -1;
                        else if (loadingValue <= 0f)
                            loadingDirection = 1;

                        sld_casting_gauge.value = loadingValue;
                        break;
                    }
            }

            if (localPlayer.State.CurrentValue == FishingPlayerState.Fighting)
            {
                FishStatus fishStatus = localPlayer.Fish.Status;

                sld_tension_gauge.value = fishStatus.currentTension;
                txt_tension_value.text = $"{fishStatus.currentTension:0.##}";
            }
        }

        void SetCastingState(CastingState state)
        {
            castingState = state;
            switch (castingState)
            {
                case CastingState.None:
                    {
                        btn_casting.gameObject.SetActive(true);
                        btn_casting.interactable = true;
                        sld_casting_gauge.gameObject.SetActive(false);
                        txt_casting.text = "Casting";
                        break;
                    }
                case CastingState.Loading:
                    {
                        sld_casting_gauge.gameObject.SetActive(true);
                        txt_casting.text = "Throw";

                        loadingDirection = 1;
                        loadingValue = 0f;
                        break;
                    }
                case CastingState.Throw:
                    {
                        btn_casting.interactable = false;
                        localPlayer.MessageBroker.Publish(new FishingEvent.OnCastingStarted() { intensity = loadingValue });
                        break;
                    }
                case CastingState.Landing:
                    {
                        btn_casting.gameObject.SetActive(false);
                        sld_casting_gauge.gameObject.SetActive(false);
                        break;
                    }
            }
        }

        void SetCastingType(CastingType type)
        {
            tgl_casting_type.SetIsOnWithoutNotify(type == CastingType.OverHead);
            txt_casting_type.text = $"{type}";
        }

        void OnClickCasting()
        {
            switch (castingState)
            {
                case CastingState.None:
                    {
                        if (localPlayer.CastingType == CastingType.OverHead)
                            SetCastingState(CastingState.Loading);
                        else
                            SetCastingState(CastingState.Throw);
                        break;
                    }
                case CastingState.Loading:
                    {
                        SetCastingState(CastingState.Throw);
                        break;
                    }
            }
        }

        void OnChangeFishingState(FishingPlayerState state)
        {
            switch (state)
            {
                case FishingPlayerState.Idle: SetCastingState(CastingState.None); break;
                case FishingPlayerState.Waiting: SetCastingState(CastingState.Landing); break;
            }

            if (state == FishingPlayerState.Fighting)
            {
                sld_tension_gauge.gameObject.SetActive(true);
            }
            else
            {
                sld_tension_gauge.gameObject.SetActive(false);
            }
        }
    }
}