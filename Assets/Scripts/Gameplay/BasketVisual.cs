using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>NTM-style 2D basket sprite with invisible 3D catch colliders.</summary>
    public sealed class BasketVisual : MonoBehaviour
    {
        const string SpriteChildName = "BasketSprite";

        const string LegacyPoleName = "Pole";

        const string LegacyRingName = "TopRing";

        [SerializeField] Sprite basketSprite;

        public static BasketVisual Ensure(Transform basket)
        {
            if (basket == null)
                return null;

            var visual = basket.GetComponent<BasketVisual>();
            if (visual == null)
                visual = basket.gameObject.AddComponent<BasketVisual>();

            visual.BuildOrRefresh();
            return visual;
        }

        void OnEnable()
        {
            if (transform.Find(SpriteChildName) == null || ResolveSprite() == null)
                BuildOrRefresh();
        }

        public void BuildOrRefresh()
        {
            HideLegacyMeshVisuals();
            EnsureSprite();
            BasketCatchDetector.Ensure(transform);
        }

        void HideLegacyMeshVisuals()
        {
            DisableRenderer(transform.Find(LegacyPoleName));
            DisableRenderer(transform.Find(LegacyRingName));
        }

        static void DisableRenderer(Transform part)
        {
            if (part == null)
                return;

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = false;
        }

        void EnsureSprite()
        {
            var sprite = ResolveSprite();
            if (sprite == null)
            {
                Debug.LogWarning("[BasketVisual] Missing basket sprite. Assign a sprite or check Art/Characters/Player/Thrower.png import.");
                return;
            }

            var spriteTf = transform.Find(SpriteChildName);
            GameObject spriteGo;

            if (spriteTf == null)
            {
                spriteGo = new GameObject(SpriteChildName);
                spriteGo.transform.SetParent(transform, false);
            }
            else
            {
                spriteGo = spriteTf.gameObject;
            }

            spriteGo.SetActive(true);

            var renderer = spriteGo.GetComponent<SpriteRenderer>() ?? spriteGo.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.enabled = true;
            renderer.sortingOrder = 20;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.maskInteraction = SpriteMaskInteraction.None;

            float targetWidth = GreyboxScale.BasketCatchDiameterM * GreyboxScale.BasketSpriteWidthScale;
            float spriteWidth = sprite.bounds.size.x;
            float uniformScale = spriteWidth > 1e-4f ? targetWidth / spriteWidth : 1f;
            spriteGo.transform.localScale = Vector3.one * uniformScale;
            spriteGo.transform.localPosition = new Vector3(
                0f,
                BasketSpriteUtil.ComputeGroundOffset(sprite, uniformScale),
                0f);

            if (spriteGo.GetComponent<BasketBillboard>() == null)
                spriteGo.AddComponent<BasketBillboard>();
        }

        Sprite ResolveSprite()
        {
            if (basketSprite != null)
                return basketSprite;

            var texture = RuntimeArt.LoadBasketTexture();
            if (texture != null)
            {
                basketSprite = BasketSpriteUtil.CreateGroundAlignedSprite(texture);
                if (basketSprite != null)
                    return basketSprite;
            }

            basketSprite = RuntimeArt.LoadBasketSprite();
            return basketSprite;
        }
    }
}
