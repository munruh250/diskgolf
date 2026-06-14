using DiskGolf.Gameplay;
using DiskGolf.UI;
using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Pulsating disc-sized landing marker and yardage label during trajectory zoom.</summary>
    public sealed class TrajectoryLandingMarker : MonoBehaviour
    {
        const int RingSegments = 36;

        const float GroundLift = 0.04f;

        const float PulseSpeed = 3.2f;

        const float PulseScaleRange = 0.22f;

        [SerializeField] LineRenderer ring;

        [SerializeField] TextMeshPro yardLabel;

        [SerializeField] Transform labelAnchor;

        const float ApexLabelLift = 1.35f;

        const float ApexLabelCameraUpLift = 0.55f;

        const int ApexLabelSortingOrder = 35;

        const float LandingLabelFontSize = 3.2f;

        const float ApexLabelFontSize = 6.4f;

        bool _visible;

        bool _apexPreviewOnly;

        float _baseRadius;

        public bool IsApexPreviewOnly => _apexPreviewOnly;

        public static TrajectoryLandingMarker Ensure()
        {
            var existing = FindFirstObjectByType<TrajectoryLandingMarker>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            var root = new GameObject("TrajectoryLandingMarker");
            return root.AddComponent<TrajectoryLandingMarker>();
        }

        void Awake()
        {
            BuildIfNeeded();
            SetVisible(false);
        }

        void BuildIfNeeded()
        {
            if (ring == null)
            {
                var ringGo = new GameObject("LandingRing", typeof(LineRenderer));
                ringGo.transform.SetParent(transform, false);
                ring = ringGo.GetComponent<LineRenderer>();
                ring.loop = true;
                ring.useWorldSpace = false;
                ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ring.receiveShadows = false;
                ring.alignment = LineAlignment.View;
                ring.material = new Material(Shader.Find("Sprites/Default"));
                ring.startColor = ring.endColor = Color.white;
                ring.positionCount = RingSegments;
            }

            _baseRadius = GreyboxScale.DiscDiameterM * 0.5f;
            UpdateRingVertices(1f);

            if (yardLabel == null)
            {
                var labelGo = new GameObject("YardLabel", typeof(TextMeshPro));
                labelGo.transform.SetParent(transform, false);
                yardLabel = labelGo.GetComponent<TextMeshPro>();
                yardLabel.alignment = TextAlignmentOptions.Center;
                yardLabel.fontSize = 3.2f;
                yardLabel.color = Color.white;
                yardLabel.enableWordWrapping = false;
                HudTypography.BindFont(yardLabel);

                labelAnchor = labelGo.transform;
                labelAnchor.localPosition = new Vector3(0f, 0.1f, _baseRadius * 1.5f);

                if (labelGo.GetComponent<CameraFacingBillboard>() == null)
                    labelGo.AddComponent<CameraFacingBillboard>();
            }
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            _apexPreviewOnly = false;

            if (ring != null)
                ring.enabled = visible;

            if (yardLabel != null)
                yardLabel.gameObject.SetActive(visible);
        }

        public void UpdateLanding(Vector3 landingWorld, float yards)
        {
            BuildIfNeeded();
            _apexPreviewOnly = false;
            _visible = true;

            if (ring != null)
                ring.enabled = true;

            if (yardLabel != null)
                yardLabel.gameObject.SetActive(true);

            transform.position = landingWorld + Vector3.up * GroundLift;

            if (labelAnchor != null)
                labelAnchor.localPosition = new Vector3(0f, 0.1f, _baseRadius * 1.5f);

            if (yardLabel != null)
            {
                yardLabel.fontSize = LandingLabelFontSize;
                yardLabel.sortingOrder = 0;
                yardLabel.text = FormatYards(yards);
            }
        }

        public void UpdateApexPreview(Vector3 apexWorld, float yards)
        {
            BuildIfNeeded();
            _apexPreviewOnly = true;
            _visible = true;

            if (ring != null)
                ring.enabled = false;

            if (labelAnchor != null)
                labelAnchor.localPosition = Vector3.zero;

            if (yardLabel != null)
            {
                yardLabel.gameObject.SetActive(true);
                yardLabel.fontSize = ApexLabelFontSize;
                yardLabel.sortingOrder = ApexLabelSortingOrder;
                yardLabel.alignment = TextAlignmentOptions.Center;
                yardLabel.text = FormatYards(yards);
            }

            transform.position = ResolveApexLabelPosition(apexWorld);
        }

        Vector3 ResolveApexLabelPosition(Vector3 apexWorld)
        {
            var position = apexWorld + Vector3.up * ApexLabelLift;

            var cam = UnityEngine.Camera.main;
            if (cam != null)
                position += cam.transform.up * ApexLabelCameraUpLift;

            return position;
        }

        public void ClearApexPreview()
        {
            if (!_apexPreviewOnly)
                return;

            SetVisible(false);
        }

        static string FormatYards(float yards) => $"{Mathf.Max(0, Mathf.RoundToInt(yards))} Yards";

        void Update()
        {
            if (!_visible || _apexPreviewOnly || ring == null)
                return;

            float pulse = (Mathf.Sin(Time.time * PulseSpeed) + 1f) * 0.5f;
            float scale = 1f + PulseScaleRange * pulse;
            UpdateRingVertices(scale);

            float alpha = Mathf.Lerp(0.55f, 1f, pulse);
            var color = new Color(1f, 1f, 1f, alpha);
            ring.startColor = color;
            ring.endColor = color;
            ring.startWidth = ring.endWidth = GreyboxScale.DiscDiameterM * 0.08f;
        }

        void UpdateRingVertices(float scale)
        {
            float radius = _baseRadius * scale;

            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i / (float)RingSegments * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }
    }
}
