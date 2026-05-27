using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Basket colliders — any disc contact during flight counts as holed.</summary>
    public sealed class BasketCatchDetector : MonoBehaviour
    {
        const string CatchCollidersName = "CatchColliders";

        const string CatchVolumeName = "CatchVolume";

        const string LegacyPoleName = "Pole";

        const string LegacyRingName = "TopRing";

        [SerializeField] float catchRadiusM = 1.05f;

        public static BasketCatchDetector Ensure(Transform basket)
        {
            if (basket == null)
                return null;

            var detector = basket.GetComponent<BasketCatchDetector>();
            if (detector == null)
                detector = basket.gameObject.AddComponent<BasketCatchDetector>();

            detector.EnsureColliders();
            return detector;
        }

        public static bool TryHitSegment(Vector3 from, Vector3 to, float radius, out Vector3 hitPosition)
        {
            hitPosition = to;

            var delta = to - from;
            float distance = delta.magnitude;

            if (distance < 1e-5f)
                return false;

            var direction = delta / distance;

            if (!Physics.SphereCast(from, radius, direction, out RaycastHit hit, distance, ~0,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (hit.collider.GetComponentInParent<BasketCatchDetector>() == null)
                return false;

            hitPosition = hit.point - hit.normal * radius * 0.25f;
            return true;
        }

        public static bool ContainsPoint(Vector3 worldPoint, float radius)
        {
            var hits = Physics.OverlapSphere(worldPoint, radius, ~0, QueryTriggerInteraction.Ignore);

            foreach (var col in hits)
            {
                if (col.GetComponentInParent<BasketCatchDetector>() != null)
                    return true;
            }

            return false;
        }

        void EnsureColliders()
        {
            catchRadiusM = Mathf.Max(catchRadiusM, GreyboxScale.BasketCatchDiameterM * 0.48f);
            DisableLegacyVisualColliders();

            var collidersRoot = EnsureChild(CatchCollidersName);
            EnsureChainCatch(collidersRoot);
            EnsurePoleCatch(collidersRoot);
            EnsureTrayCatch(collidersRoot);
        }

        void DisableLegacyVisualColliders()
        {
            DisableCollider(transform.Find(LegacyPoleName));
            DisableCollider(transform.Find(LegacyRingName));
        }

        static void DisableCollider(Transform part)
        {
            if (part == null)
                return;

            var collider = part.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
        }

        void EnsureChainCatch(Transform root)
        {
            var catchVolume = EnsureChild(root, CatchVolumeName);
            catchVolume.localPosition = Vector3.up * GreyboxScale.BasketCatchHeightM;

            var sphere = catchVolume.GetComponent<SphereCollider>() ?? catchVolume.gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = false;
            sphere.radius = catchRadiusM;
            sphere.center = Vector3.zero;
        }

        static void EnsurePoleCatch(Transform root)
        {
            var pole = EnsureChild(root, "CatchPole");
            pole.localPosition = Vector3.up * (GreyboxScale.BasketCatchHeightM * 0.5f);

            var capsule = pole.GetComponent<CapsuleCollider>() ?? pole.gameObject.AddComponent<CapsuleCollider>();
            capsule.isTrigger = false;
            capsule.radius = GreyboxScale.PoleDiameterM * 0.55f;
            capsule.height = GreyboxScale.BasketCatchHeightM;
            capsule.direction = 1;
            capsule.center = Vector3.zero;
        }

        static void EnsureTrayCatch(Transform root)
        {
            var tray = EnsureChild(root, "CatchTray");
            tray.localPosition = Vector3.up * (GreyboxScale.BasketCatchHeightM * 0.38f);

            var sphere = tray.GetComponent<SphereCollider>() ?? tray.gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = false;
            sphere.radius = GreyboxScale.BasketCatchDiameterM * 0.34f;
            sphere.center = Vector3.zero;
        }

        Transform EnsureChild(string name)
        {
            var existing = transform.Find(name);
            if (existing != null)
                return existing;

            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        static Transform EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
                return existing;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }
    }
}
