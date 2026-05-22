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

        [SerializeField] TextMeshProUGUI discText;

        [SerializeField] TextMeshProUGUI stanceText;

        [SerializeField] TextMeshProUGUI windText;

        void LateUpdate()
        {
            if (hole == null || discTransform == null)
                return;

            int restFt = Mathf.Max(0, Mathf.RoundToInt(hole.DistanceToBasket(discTransform.position)));

            if (restText != null)
                restText.text = $"REST {restFt}ft";

            var active = controller != null ? controller.ActiveDisc : null;

            if (discText != null)
            {
                discText.text = active != null
                    ? $"{active.displayName} {active.speed}/{active.glide}/{active.turn}/{active.fade}"
                    : "—";
            }

            if (stanceText != null)
                stanceText.text = controller != null
                    ? controller.ReleaseAngle.ToString().ToUpperInvariant()
                    : "FLAT";

            if (windText != null && controller != null)
            {
                var w = controller.Wind;

                windText.text = $"WIND {(int)Mathf.Round(w.speedMph)} mph";
            }
        }
    }
}
