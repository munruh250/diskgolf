using DiskGolf.UI;
using DiskGolf.UI.Callouts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.Tests.EditMode
{
    public sealed class MultiSlotSpriteBannerTests
    {
        [Test]
        public void ShowKind_ActivatesOnlyMatchingSlot()
        {
            var root = new GameObject("Banner", typeof(RectTransform), typeof(MultiSlotSpriteBanner));
            var banner = root.GetComponent<MultiSlotSpriteBanner>();

            var parGo = new GameObject("Par", typeof(RectTransform), typeof(Image));
            parGo.transform.SetParent(root.transform, false);
            var birdieGo = new GameObject("Birdie", typeof(RectTransform), typeof(Image));
            birdieGo.transform.SetParent(root.transform, false);

            banner.ConfigureSlotsForTests(new[]
            {
                (HoleCompleteScoreKind.Par, parGo.GetComponent<Image>()),
                (HoleCompleteScoreKind.Birdie, birdieGo.GetComponent<Image>()),
            });

            banner.ShowKind(HoleCompleteScoreKind.Birdie);

            Assert.IsFalse(parGo.activeSelf);
            Assert.IsTrue(birdieGo.activeSelf);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Hide_DeactivatesRootAfterPreviewShow()
        {
            var root = new GameObject("Banner", typeof(RectTransform), typeof(MultiSlotSpriteBanner));
            var banner = root.GetComponent<MultiSlotSpriteBanner>();

            var parGo = new GameObject("Par", typeof(RectTransform), typeof(Image));
            parGo.transform.SetParent(root.transform, false);

            banner.ConfigureSlotsForTests(new[]
            {
                (HoleCompleteScoreKind.Par, parGo.GetComponent<Image>()),
            });

            banner.ShowKind(HoleCompleteScoreKind.Par);
            banner.Hide();

            Assert.IsFalse(root.activeSelf);
            Assert.IsFalse(parGo.activeSelf);

            Object.DestroyImmediate(root);
        }
    }
}
