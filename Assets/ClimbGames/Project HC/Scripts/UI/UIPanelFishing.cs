using UnityEngine;
using ClimbGames.UI;
using UnityEngine.UI;
using TMPro;
using R3;
using R3.Triggers;

namespace ClimbGames.Project_HC
{
    public class UIPanelFishing : UIBase
    {
        enum CastingState
        {
            None,
            Loading,
            Throw,
            Landing,
        }

        [SerializeField] private VariableJoystick joystick;
        [SerializeField] private Button btn_casting;
        [SerializeField] private Slider sld_casting_gauge;
        [SerializeField] private TMP_Text txt_casting;
        [SerializeField] private Button btn_reeling;
        [SerializeField] private Toggle tgl_casting_type;
        [SerializeField] private TMP_Text txt_casting_type;
        [SerializeField] private Slider sld_tension_gauge;
        [SerializeField] private TMP_Text txt_tension_value;
        [SerializeField] private Button btn_get;

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

            localPlayer.Model.ActorState.Subscribe(_ => OnChangeFishingState(_)).AddTo(this);
            btn_casting.OnClickAsObservable().Subscribe(_ => OnClickCasting()).AddTo(this);

            SetCastingType(localPlayer.Model.CastingType.CurrentValue);
            tgl_casting_type.OnValueChangedAsObservable().Subscribe(_ =>
            {
                CastingType type = _ ? CastingType.OverHead : CastingType.Pitching;
                localPlayer.Model.ChangeCastingType(type);
                SetCastingType(type);
            }).AddTo(this);

            btn_reeling.OnPointerDownAsObservable().Subscribe(_ => localPlayer.Model.SetReelingInput(true)).AddTo(this);
            btn_reeling.OnPointerUpAsObservable().Subscribe(_ => localPlayer.Model.SetReelingInput(false)).AddTo(this);

            localPlayer.MessageBroker.Receive<FishingEvent.OnFishCatched>().Subscribe(_ => btn_get.gameObject.SetActive(true)).AddTo(this);
            btn_get.OnClickAsObservable().Subscribe(_ => localPlayer.TakeHookedFish()).AddTo(this);
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
                        localPlayer.Model.SetCastingIntensity(loadingValue);
                        break;
                    }
            }

            if (localPlayer.Model.ActorState.CurrentValue == FishingActorState.Fighting)
            {
                FishingPlayerModel model = localPlayer.Model;

                sld_tension_gauge.value = model.currentTension;
                txt_tension_value.text = $"{model.currentTension:0.##}";
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

                        btn_get.gameObject.SetActive(false);
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
                        localPlayer.StartCasting(loadingValue);
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
                        if (localPlayer.Model.CastingType.CurrentValue == CastingType.OverHead)
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

        void OnChangeFishingState(FishingActorState state)
        {
            switch (state)
            {
                case FishingActorState.Idle: SetCastingState(CastingState.None); break;
                case FishingActorState.Waiting: SetCastingState(CastingState.Landing); break;
            }

            if (state == FishingActorState.Fighting)
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