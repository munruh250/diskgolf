using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DiskGolf.Gameplay
{
    /// <summary>NTM-style 2D basket sprite with invisible 3D catch colliders.</summary>
    public sealed class BasketVisual : MonoBehaviour
    {
        const string SpriteResourcePath = "Basket/2dbucket";

        const string SpriteAssetPath = "Assets/Resources/Basket/2dbucket.png";

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
                Debug.LogWarning("[BasketVisual] Missing sprite at Resources/Basket/2dbucket");
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
            spriteGo.transform.localPosition = new Vector3(0f, 0.03f, 0f);

            if (spriteGo.GetComponent<BasketBillboard>() == null)
                spriteGo.AddComponent<BasketBillboard>();
        }

        Sprite ResolveSprite()
        {
            if (basketSprite != null)
                return basketSprite;

            basketSprite = Resources.Load<Sprite>(SpriteResourcePath);
            if (basketSprite != null)
                return basketSprite;

#if UNITY_EDITOR
            basketSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteAssetPath);
            if (basketSprite != null)
                return basketSprite;
#endif

            var texture = Resources.Load<Texture2D>(SpriteResourcePath);
            if (texture == null)
                return null;

            basketSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0f),
                220f);

            return basketSprite;
        }
    }
}
