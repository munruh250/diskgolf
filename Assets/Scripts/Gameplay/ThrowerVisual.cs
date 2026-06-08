using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>NTM-style 2D thrower sprite with a hand anchor for the disc.</summary>
    public sealed class ThrowerVisual : MonoBehaviour
    {
        const float MaxAimLeanDegrees = 20f;

        [SerializeField] Transform handAnchor;

        [SerializeField] Sprite throwerSprite;

        Transform _spriteTransform;

        public Transform HandAnchor => handAnchor;

        void Awake()
        {
            BindHandAnchor();
            _spriteTransform ??= transform.Find("Sprite");
            ApplySortingOrder();
        }

        void BindHandAnchor() => handAnchor ??= transform.Find("HandAnchor");

        /// <summary>Default pose for a newly created hand anchor only.</summary>
        void ApplyDefaultHandAnchorPose()
        {
            if (handAnchor == null)
                return;

            handAnchor.localPosition = new Vector3(0.18f, 1.20f, 0.06f);
            handAnchor.localRotation = Quaternion.Euler(-16f, 0f, 0f);
        }

        /// <summary>Layout sprite child when first building the rig — does not move HandAnchor.</summary>
        void ApplyNewSpriteLayout(Transform spriteTf, SpriteRenderer renderer)
        {
            if (spriteTf == null)
                return;

            spriteTf.localPosition = new Vector3(0f, GreyboxScale.ThrowerSpriteLocalY, 0f);

            if (renderer?.sprite != null)
            {
                float targetHeight = 1.75f;
                float spriteHeight = renderer.sprite.bounds.size.y;
                float scale = spriteHeight > 1e-4f ? targetHeight / spriteHeight : 1f;
                spriteTf.localScale = Vector3.one * scale;
            }
        }

        public void RebuildAsSprite()
        {
            ClearLegacyRig();
            BuildSpriteRig();
        }

        public static ThrowerVisual Build(Vector3 position, Quaternion facingBasket)
        {
            var root = new GameObject("Thrower");
            root.transform.SetPositionAndRotation(position, facingBasket);

            var visual = root.AddComponent<ThrowerVisual>();
            visual.BuildSpriteRig();
            return visual;
        }

        void BuildSpriteRig()
        {
            ClearLegacyRig();

            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(transform, false);
            spriteGo.transform.localPosition = new Vector3(0f, GreyboxScale.ThrowerSpriteLocalY, 0f);

            var renderer = spriteGo.AddComponent<SpriteRenderer>();
            renderer.sprite = ResolveSprite();
            renderer.sortingOrder = 30;
            renderer.flipX = false;

            if (renderer.sprite != null)
            {
                ApplyNewSpriteLayout(spriteGo.transform, renderer);
            }
            else
            {
                Debug.LogWarning("[ThrowerVisual] Missing thrower sprite. Assign throwerSprite or add Art/Characters/Player/Thrower.png.");
            }

            spriteGo.AddComponent<ThrowerBillboard>();
            _spriteTransform = spriteGo.transform;
            ApplySortingOrder();

            BindHandAnchor();
            if (handAnchor == null)
            {
                handAnchor = new GameObject("HandAnchor").transform;
                handAnchor.SetParent(transform, false);
                ApplyDefaultHandAnchorPose();
            }
        }

        void ClearLegacyRig()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            handAnchor = null;
            _spriteTransform = null;
        }

        void ApplySortingOrder()
        {
            if (_spriteTransform == null)
                return;

            var renderer = _spriteTransform.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.sortingOrder = 30;
        }

        /// <summary>Subtle Y-lean on the 2D sprite toward the aim line (±20° max).</summary>
        public void ApplyAimLean(float yawOffsetDegrees)
        {
            _spriteTransform ??= transform.Find("Sprite");
            if (_spriteTransform == null)
                return;

            float lean = Mathf.Clamp(yawOffsetDegrees, -MaxAimLeanDegrees, MaxAimLeanDegrees);
            _spriteTransform.localRotation = Quaternion.Euler(0f, lean, 0f);
        }

        Sprite ResolveSprite()
        {
            if (throwerSprite != null)
                return throwerSprite;

            throwerSprite = RuntimeArt.LoadThrowerSprite();
            return throwerSprite;
        }
    }
}
