using DiskGolf.Core;
using DiskGolf.Disc;
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

        public void Configure(HoleDataAsset hole, ThemePack themePack, HoleSetup setup)
        {
            playtestHole = hole;
            theme = themePack;
            holeSetup = setup;
        }

        void Awake()
        {
            var bag = FindFirstObjectByType<DiscBag>();
            DiscBagBootstrap.EnsurePopulated(bag);

            if (playtestHole?.Data == null || theme == null)
                return;

            var host = CourseBuilder.Build(playtestHole.Data, theme);
            if (host == null)
                return;

            holeSetup ??= FindFirstObjectByType<HoleSetup>();
            if (holeSetup == null)
                return;

            var data = playtestHole.Data;
            holeSetup.BindBuiltCourse(
                host.TeePad,
                host.Basket,
                data.Hole.Par,
                data.Hole.CircleRadiusFt,
                data.HoleLengthYards() * 3f);

            holeSetup.PositionThrowerAtTee();
            holeSetup.RefreshCameraAimPoint();
        }
    }
}
