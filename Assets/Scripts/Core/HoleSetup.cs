using DiskGolf.Flight;
using UnityEngine;

namespace DiskGolf.Core
{
    public class HoleSetup : MonoBehaviour
    {
        [SerializeField] Transform teePad;
        [SerializeField] Transform basket;
        [SerializeField] float circleRadiusFt = 33f;
        [SerializeField] float holeLengthFt = 250f;

        public Vector3 TeePosition => teePad != null ? teePad.position : Vector3.zero;

        public Vector3 BasketPosition => basket != null ? basket.position : Vector3.forward * 76.2f;

        public float CircleRadiusFt => circleRadiusFt;

        /// <summary>Hole length tuning (inspector); visuals use tee/basket positions.</summary>
        public float HoleLengthFt => holeLengthFt;

        public Vector3 AimDirection
        {
            get
            {
                var delta = BasketPosition - TeePosition;
                return delta.sqrMagnitude < 1e-8f ? Vector3.forward : delta.normalized;
            }
        }

        public float DistanceToBasket(Vector3 from) =>
            Vector3.Distance(from, BasketPosition) / 0.3048f;

        public WindSettings RollWind()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            return new WindSettings
            {
                direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)),
                speedMph = Random.Range(0f, 15f)
            };
        }
    }
}
