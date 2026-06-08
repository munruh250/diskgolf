using DiskGolf.Core;
using DiskGolf.Disc;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Selected character portrait in the power-meter hub (NTM-style mood portrait).</summary>
    public sealed class PowerMeterPortraitWidget : MonoBehaviour
    {
        public const string ObjectName = "PlayerPortraitMood";

        static readonly Color FallbackColor = new(0.35f, 0.55f, 0.85f, 1f);

        [SerializeField] Image portraitImage;

        void OnEnable()
        {
            BindReferences();
            Refresh();
        }

        public void BindReferences()
        {
            portraitImage ??= GetComponent<Image>();
            Refresh();
        }

        public void Refresh()
        {
            if (portraitImage == null)
                return;

            ApplyCharacter(GameSessionSettings.ActiveCharacter);
        }

        void ApplyCharacter(PlayerCharacterProfile character)
        {
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;

            if (character?.previewSprite != null)
            {
                portraitImage.sprite = character.previewSprite;
                portraitImage.color = Color.white;
                portraitImage.enabled = true;
                return;
            }

            portraitImage.sprite = null;
            portraitImage.color = character != null ? character.portraitColor : FallbackColor;
            portraitImage.enabled = true;
        }

        public static PowerMeterPortraitWidget EnsureInArcHub(RectTransform arcHub)
        {
            if (arcHub == null)
                return null;

            CleanupStrayPortraits(arcHub);

            var hub = EnsureHub(arcHub);
            CleanupHubDecorations(hub);
            ConfigureHubLayout(hub);

            var portrait = EnsurePortrait(hub);

            var needle = arcHub.Find("Needle");
            if (needle != null)
                needle.SetAsLastSibling();

            portrait.BindReferences();
            return portrait;
        }

        static RectTransform EnsureHub(RectTransform arcHub)
        {
            var existing = arcHub.Find("Hub") as RectTransform;
            if (existing != null)
                return existing;

            var hubGo = new GameObject("Hub", typeof(RectTransform));
            var hubRt = hubGo.GetComponent<RectTransform>();
            hubRt.SetParent(arcHub, false);
            hubRt.SetSiblingIndex(2);
            return hubRt;
        }

        static void CleanupHubDecorations(RectTransform hub)
        {
            DestroyChild(hub, "HubBackdrop");
            DestroyChild(hub, "HubRing");
            DestroyLegacyDiscPreview(hub);
        }

        static void CleanupStrayPortraits(RectTransform arcHub)
        {
            var hub = arcHub.Find("Hub");

            for (int i = arcHub.childCount - 1; i >= 0; i--)
            {
                var child = arcHub.GetChild(i);
                if (child.name != ObjectName || child == hub)
                    continue;

                DestroyObject(child.gameObject);
            }
        }

        static void DestroyLegacyDiscPreview(RectTransform hub)
        {
            var legacy = hub.Find("DiscPreview");
            if (legacy == null)
                return;

            DestroyObject(legacy.gameObject);
        }

        static void ConfigureHubLayout(RectTransform hub)
        {
            hub.anchorMin = hub.anchorMax = new Vector2(0.5f, 0f);
            hub.pivot = new Vector2(0.5f, 0.5f);
            hub.anchoredPosition = Vector2.zero;
            hub.sizeDelta = new Vector2(
                TimingMeterLayout.PowerMeterPortraitWidth,
                TimingMeterLayout.PowerMeterPortraitHeight);
            hub.localScale = Vector3.one;
        }

        static PowerMeterPortraitWidget EnsurePortrait(RectTransform hub)
        {
            var portrait = DedupePortraits(hub);
            if (portrait != null)
            {
                ConfigurePortraitRect(portrait.transform as RectTransform);
                return portrait;
            }

            return CreatePortrait(hub);
        }

        static PowerMeterPortraitWidget DedupePortraits(RectTransform hub)
        {
            PowerMeterPortraitWidget keep = null;
            var duplicates = new List<GameObject>();

            for (int i = 0; i < hub.childCount; i++)
            {
                var child = hub.GetChild(i);
                if (child.name != ObjectName)
                    continue;

                if (keep == null)
                {
                    keep = ResolvePortraitWidget(child);
                    continue;
                }

                duplicates.Add(child.gameObject);
            }

            foreach (var duplicate in duplicates)
                DestroyObject(duplicate);

            return keep;
        }

        static PowerMeterPortraitWidget ResolvePortraitWidget(Transform portraitTransform)
        {
            var widget = portraitTransform.GetComponent<PowerMeterPortraitWidget>();
            if (widget == null)
                widget = portraitTransform.gameObject.AddComponent<PowerMeterPortraitWidget>();

            var image = portraitTransform.GetComponent<Image>();
            if (image == null)
                image = portraitTransform.gameObject.AddComponent<Image>();

            image.preserveAspect = true;
            image.raycastTarget = false;
            widget.portraitImage = image;
            return widget;
        }

        static PowerMeterPortraitWidget CreatePortrait(RectTransform hub)
        {
            var go = new GameObject(ObjectName, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(hub, false);
            ConfigurePortraitRect(rt);

            var widget = go.GetComponent<PowerMeterPortraitWidget>();
            if (widget == null)
                widget = go.AddComponent<PowerMeterPortraitWidget>();

            widget.portraitImage = go.GetComponent<Image>();
            return widget;
        }

        static void ConfigurePortraitRect(RectTransform rt)
        {
            if (rt == null)
                return;

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        static void DestroyChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child == null)
                return;

            DestroyObject(child.gameObject);
        }

        static void DestroyObject(Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }
    }
}
