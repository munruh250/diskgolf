using DiskGolf.Core;
using DiskGolf.Gameplay;
using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Gameplay HUD fed from ThrowController and HoleSetup.</summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] ThrowController controller;

        [SerializeField] HoleSetup hole;

        [SerializeField] Transform discTransform;

        [SerializeField] DiscFlightPresenter flightPresenter;

        [SerializeField] TextMeshProUGUI restText;

        [SerializeField] TextMeshProUGUI scoreText;

        [SerializeField] TextMeshProUGUI discHeightText;

        [SerializeField] TextMeshProUGUI discText;

        [SerializeField] TextMeshProUGUI stanceText;

        [SerializeField] TextMeshProUGUI windText;

        RestDriveReadout _restDrive;

        HoleInfoPanel _holeInfo;

        WindWidget _windWidget;

        ThrowPhase _lastPhase;

        Vector3 _throwOriginWorld;

        float _lastCompletedThrowFt;

        void OnEnable()
        {
            HudLayoutSettings.EnsureOnHudRoot();
            TimingMeterHud.Ensure();
            flightPresenter ??= FindObjectOfType<DiscFlightPresenter>();
            ResolveLegacyLabelRefs();

            var hudRoot = GameObject.Find("GameplayHUD")?.GetComponent<RectTransform>();
            _restDrive = RestDriveReadout.Ensure(hudRoot);
            _holeInfo = HoleInfoPanel.Ensure(hudRoot);
            _windWidget = WindWidget.Ensure(hudRoot);

            HideLegacyLabels();
        }

        void HideLegacyLabels()
        {
            if (restText != null)
                restText.gameObject.SetActive(false);

            if (scoreText != null)
                scoreText.gameObject.SetActive(false);

            if (windText != null)
                windText.gameObject.SetActive(false);

            if (discHeightText != null)
                discHeightText.gameObject.SetActive(false);
        }

        void ResolveLegacyLabelRefs()
        {
            var hud = GameObject.Find("GameplayHUD")?.transform;
            if (hud == null)
                return;

            discText ??= FindLabel(hud, "Disc");
            stanceText ??= FindLabel(hud, "StanceLabel") ?? FindLabel(hud, "TypeThrow") ?? FindLabel(hud, "FLAT");
        }

        static TextMeshProUGUI FindLabel(Transform root, string name)
        {
            var tf = root.Find(name);
            return tf != null ? tf.GetComponent<TextMeshProUGUI>() : null;
        }

        void LateUpdate()
        {
            UpdatePhaseTracking();
            UpdateRestDrive();
            UpdateHoleInfo();
            UpdateWind();
            UpdateDiscAndStance();
        }

        void UpdatePhaseTracking()
        {
            if (controller == null)
                return;

            var phase = controller.Phase;
            var discPos = ResolveDiscWorldPosition();

            if (_lastPhase != ThrowPhase.InFlight && phase == ThrowPhase.InFlight)
                _throwOriginWorld = discPos;

            if (_lastPhase == ThrowPhase.InFlight && phase != ThrowPhase.InFlight)
                _lastCompletedThrowFt = HorizontalThrowDistanceFt(_throwOriginWorld, discPos);

            _lastPhase = phase;
        }

        void UpdateRestDrive()
        {
            if (_restDrive == null || hole == null)
                return;

            var discPos = ResolveDiscWorldPosition();
            float basketFt = hole.DisplayDistanceToBasketFt(discPos);
            float throwFt = ResolveThrowDistanceFt(discPos);
            int heightFt = Mathf.Max(0, Mathf.RoundToInt(discPos.y * 3.28084f));

            _restDrive.SetValues(FeetToYards(basketFt), FeetToYards(throwFt), heightFt);
        }

        float ResolveThrowDistanceFt(Vector3 discPos)
        {
            if (controller == null)
                return 0f;

            if (controller.Phase == ThrowPhase.InFlight)
                return HorizontalThrowDistanceFt(_throwOriginWorld, discPos);

            if (controller.Phase is ThrowPhase.Landed or ThrowPhase.Resolve)
                return _lastCompletedThrowFt;

            return 0f;
        }

        void UpdateHoleInfo()
        {
            if (_holeInfo == null || hole == null)
                return;

            int holeYards = FeetToYards(hole.HoleLengthDisplayFt);
            _holeInfo.SetHoleInfo(hole.HoleNumber, holeYards, hole.Par);
        }

        void UpdateWind()
        {
            if (_windWidget == null || controller == null)
                return;

            _windWidget.SetWind(controller.Wind);
        }

        void UpdateDiscAndStance()
        {
            if (hole == null)
                return;

            var active = controller != null ? controller.ActiveDisc : null;

            if (discText != null)
            {
                discText.text = active != null
                    ? $"{active.displayName}  {active.speed}/{active.glide}/{active.turn}/{active.fade}"
                    : "—";
            }

            if (stanceText != null)
            {
                stanceText.text = controller != null
                    ? controller.ReleaseAngle.ToString().ToUpperInvariant()
                    : "FLAT";
            }
        }

        Vector3 ResolveDiscWorldPosition()
        {
            if (controller != null && controller.Phase == ThrowPhase.InFlight)
            {
                if (flightPresenter != null && flightPresenter.DiscTransform != null)
                    return flightPresenter.DiscTransform.position;

                if (discTransform != null)
                    return discTransform.position;
            }

            if (discTransform != null)
                return discTransform.position;

            return controller != null ? controller.CurrentDiscWorld : Vector3.zero;
        }

        static float HorizontalThrowDistanceFt(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to) / 0.3048f;
        }

        static int FeetToYards(float feet) => Mathf.Max(0, Mathf.RoundToInt(feet / 3f));
    }
}
