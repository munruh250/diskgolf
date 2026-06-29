using DiskGolf.Core;
using DiskGolf.Disc;
using DiskGolf.UI.CourseEditor;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    /// <summary>Builds the painted hole from scratch data when entering play mode in the Course Editor scene.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class CourseEditorRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] HoleDataAsset playtestHole;

        [SerializeField] ThemePack theme;

        [SerializeField] HoleSetup holeSetup;

        HoleData runtimeHole;

        public void Configure(HoleDataAsset hole, ThemePack themePack, HoleSetup setup)
        {
            playtestHole = hole;
            runtimeHole = null;
            theme = themePack;
            holeSetup = setup;
        }

        public void Configure(HoleData data, ThemePack themePack, HoleSetup setup)
        {
            playtestHole = null;
            runtimeHole = data;
            theme = themePack;
            holeSetup = setup;
        }

        void Awake()
        {
            var bag = FindFirstObjectByType<DiscBag>();
            DiscBagBootstrap.EnsurePopulated(bag);

            HoleData data = runtimeHole ?? playtestHole?.Data;
            if (data == null)
            {
                string id = PlayerPrefs.GetString(CourseEditorNavigation.ActiveHoleIdKey, null);
                if (!string.IsNullOrEmpty(id))
                {
                    data = HoleDataCatalog.Player.Load(id);
                    theme ??= ThemePackLoader.Load(data.ThemeId);
                }
            }

            if (data == null || theme == null)
                return;

            var host = CourseBuilder.Build(data, theme);
            if (host == null)
                return;

            holeSetup ??= FindFirstObjectByType<HoleSetup>();
            if (holeSetup == null)
                return;

            holeSetup.BindBuiltCourse(
                host.TeePad,
                host.Basket,
                data.Hole.Par,
                data.Hole.CircleRadiusFt,
                data.HoleLengthYards() * 3f);

            holeSetup.PositionThrowerAtTee();
            holeSetup.RefreshCameraAimPoint();

            SceneEnvironmentApplier.Apply(data, theme);
        }
    }
}
