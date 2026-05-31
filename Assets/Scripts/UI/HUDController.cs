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

        float _restAtThrowStartFt;

        float _lastCompletedDriveFt;

        void OnEnable()
        {
            TimingMeterHud.Ensure();
            NtmHudLayout.Apply();

            var hudRoot = GameObject.Find("GameplayHUD")?.GetComponent<RectTransform>();
            _restDrive = NtmRestDriveReadout.Ensure(hudRoot);
            _holeInfo = NtmHoleInfoPanel.Ensure(hudRoot);
            _windWidget = NtmWindWidget.Ensure(hudRoot);

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
            if (controller == null)
                return;

            var phase = controller.Phase;

            if (_lastPhase != ThrowPhase.InFlight && phase == ThrowPhase.InFlight && hole != null && discTransform != null)
                _restAtThrowStartFt = hole.DistanceToBasket(discTransform.position);

            if (_lastPhase == ThrowPhase.InFlight && phase != ThrowPhase.InFlight && hole != null && discTransform != null)
            {
                float restFt = hole.DistanceToBasket(discTransform.position);
                _lastCompletedDriveFt = Mathf.Max(0f, _restAtThrowStartFt - restFt);
            }

            _lastPhase = phase;
        }

        void UpdateRestDrive()
        {
            if (_restDrive == null || hole == null || discTransform == null)
                return;

            float restFt;
            float driveFt;

            if (controller != null && controller.Phase == ThrowPhase.InFlight)
            {
                restFt = hole.DistanceToBasket(discTransform.position);
                driveFt = Mathf.Max(0f, _restAtThrowStartFt - restFt);
            }
            else if (controller != null && controller.ShowsTrajectoryPreview)
            {
                float lieRestFt = hole.DistanceToBasket(discTransform.position);
                driveFt = controller.TargetTrajectoryFt;
                restFt = Mathf.Max(0f, lieRestFt - driveFt);
            }
            else
            {
                restFt = hole.DistanceToBasket(discTransform.position);
                driveFt = _lastCompletedDriveFt;
            }

            _restDrive.SetValues(FeetToYards(restFt), FeetToYards(driveFt));
        }

        void UpdateHoleInfo()
        {
            if (_holeInfo == null || hole == null)
                return;

            int totalYards = FeetToYards(hole.DistanceToBasket(hole.TeePosition));
            _holeInfo.SetHoleInfo(hole.HoleNumber, totalYards, hole.Par);
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

        static int FeetToYards(float feet) => Mathf.Max(0, Mathf.RoundToInt(feet / 3f));
    }
}
