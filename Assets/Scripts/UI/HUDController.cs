using DiskGolf.Core;
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

        [SerializeField] TextMeshProUGUI restText;

        [SerializeField] TextMeshProUGUI scoreText;

        [SerializeField] TextMeshProUGUI discHeightText;

        [SerializeField] TextMeshProUGUI discText;

        [SerializeField] TextMeshProUGUI stanceText;

        [SerializeField] TextMeshProUGUI windText;

        NtmRestDriveReadout _restDrive;

        NtmHoleInfoPanel _holeInfo;

        NtmWindWidget _windWidget;

        ThrowPhase _lastPhase;

        Vector3 _throwOriginWorld;

        float _lastCompletedThrowFt;

        void OnEnable()
        {
            TimingMeterHud.Ensure();

            var hudRoot = GameObject.Find("GameplayHUD")?.GetComponent<RectTransform>();
            _restDrive = NtmRestDriveReadout.Ensure(hudRoot);
            _holeInfo = NtmHoleInfoPanel.Ensure(hudRoot);
            _windWidget = NtmWindWidget.Ensure(hudRoot);

            HideLegacyLabels();
            NtmHudLayout.Apply();
        }

        void HideLegacyLabels()
        {
            if (restText != null)
                restText.gameObject.SetActive(false);

            if (scoreText != null)
                scoreText.gameObject.SetActive(false);

            if (windText != null)
                windText.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            UpdatePhaseTracking();
            UpdateRestDrive();
            UpdateHoleInfo();
            UpdateWind();
            UpdateDiscHeight();
            UpdateDiscAndStance();
        }

        void UpdatePhaseTracking()
        {
            if (controller == null || discTransform == null)
                return;

            var phase = controller.Phase;

            if (_lastPhase != ThrowPhase.InFlight && phase == ThrowPhase.InFlight)
                _throwOriginWorld = discTransform.position;

            if (_lastPhase == ThrowPhase.InFlight && phase != ThrowPhase.InFlight)
                _lastCompletedThrowFt = HorizontalThrowDistanceFt(_throwOriginWorld, discTransform.position);

            _lastPhase = phase;
        }

        void UpdateRestDrive()
        {
            if (_restDrive == null || hole == null || discTransform == null)
                return;

            float basketFt = hole.DisplayDistanceToBasketFt(discTransform.position);
            float throwFt = ResolveThrowDistanceFt();

            _restDrive.SetValues(FeetToYards(basketFt), FeetToYards(throwFt));
        }

        float ResolveThrowDistanceFt()
        {
            if (controller == null)
                return 0f;

            if (controller.Phase == ThrowPhase.InFlight)
                return HorizontalThrowDistanceFt(_throwOriginWorld, discTransform.position);

            if (controller.ShowsTrajectoryPreview)
                return controller.TargetTrajectoryFt;

            return _lastCompletedThrowFt;
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

        void UpdateDiscHeight()
        {
            if (discHeightText == null || discTransform == null)
                return;

            int heightFt = Mathf.Max(0, Mathf.RoundToInt(discTransform.position.y * 3.28084f));
            discHeightText.text = $"DISC HEIGHT {heightFt}ft";
        }

        void UpdateDiscAndStance()
        {
            if (hole == null || discTransform == null)
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

        static float HorizontalThrowDistanceFt(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to) / 0.3048f;
        }

        static int FeetToYards(float feet) => Mathf.Max(0, Mathf.RoundToInt(feet / 3f));
    }
}
