using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>NTM-style 2D thrower sprite with a hand anchor for the disc.</summary>
    public sealed class ThrowerVisual : MonoBehaviour
    {
        [SerializeField] Transform handAnchor;

        [SerializeField] Sprite throwerSprite;

        public Transform HandAnchor => handAnchor;

        public void ApplyReachBackHandPose()
        {
            handAnchor ??= transform.Find("HandAnchor");
            if (handAnchor == null)
                return;

            handAnchor.localPosition = new Vector3(0.18f, 1.20f, 0.06f);
            handAnchor.localRotation = Quaternion.Euler(-16f, 0f, 0f);
        }

        /// <summary>Apply sprite height/offset without rebuilding the rig.</summary>
        public void ApplySpriteLayout()
        {
            var spriteTf = transform.Find("Sprite");
            if (spriteTf == null)
                return;

            spriteTf.localPosition = new Vector3(0f, GreyboxScale.ThrowerSpriteLocalY, 0f);

            var renderer = spriteTf.GetComponent<SpriteRenderer>();
            if (renderer?.sprite != null)
            {
                float targetHeight = 1.75f;
                float spriteHeight = renderer.sprite.bounds.size.y;
                float scale = spriteHeight > 1e-4f ? targetHeight / spriteHeight : 1f;
                spriteTf.localScale = Vector3.one * scale;
            }

            ApplyReachBackHandPose();
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
            renderer.sortingOrder = 20;
            renderer.flipX = false;

            if (renderer.sprite != null)
            {
                float targetHeight = 1.75f;
                float spriteHeight = renderer.sprite.bounds.size.y;
                float scale = spriteHeight > 1e-4f ? targetHeight / spriteHeight : 1f;
                spriteGo.transform.localScale = Vector3.one * scale;
            }
            else
            {
                Debug.LogWarning("[ThrowerVisual] Missing thrower sprite. Run Disk Golf → Refresh Gameplay Art Catalog.");
            }

            spriteGo.AddComponent<ThrowerBillboard>();

            handAnchor = new GameObject("HandAnchor").transform;
            handAnchor.SetParent(transform, false);
            ApplyReachBackHandPose();
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
