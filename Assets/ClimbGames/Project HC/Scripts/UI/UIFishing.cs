using UnityEngine;
using ClimbGames.UI;
using UnityEngine.UI;
using TMPro;
using R3;

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

        private CastingState castingState = CastingState.None;
        private int loadingDirection = 1;
        private float loadingValue;

        void OnEnable()
        {
            SetCastingState(CastingState.None);
        }

        void Start()
        {
            btn_casting.OnClickAsObservable().Subscribe(_ =>
            {
                switch (castingState)
                {
                    case CastingState.None: SetCastingState(CastingState.Loading); break;
                    case CastingState.Loading: SetCastingState(CastingState.Throw); break;
                }
            }).AddTo(this);

            SetCastingType(FishingModel.Instance.CastingType.CurrentValue);
            tgl_casting_type.OnValueChangedAsObservable().Subscribe(_ =>
            {
                SetCastingType(_ ? CastingType.OverHead : CastingType.Pitching);
            }).AddTo(this);
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
                case CastingState.Throw:
                    {
                        if (FishingActor.Instance.State == FishingActorState.Waiting)
                            SetCastingState(CastingState.Landing);
                        break;
                    }
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

                        // nofity throw
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
            FishingModel.Instance.ChangeCastingType(type);

            tgl_casting_type.SetIsOnWithoutNotify(type == CastingType.OverHead);
            txt_casting_type.text = $"{type}";
        }
    }
}