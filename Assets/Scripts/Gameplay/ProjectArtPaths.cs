namespace DiskGolf.Gameplay
{
    /// <summary>Canonical asset paths — use when creating or loading project art.</summary>
    public static class ProjectArtPaths
    {
        public const string ArtRoot = "Assets/Art";

        public static class Characters
        {
            public const string PlayerRoot = ArtRoot + "/Characters/Player";

            public const string ThrowerSprite = PlayerRoot + "/Thrower.png";
        }

        public static class Environment
        {
            public static class Basket
            {
                public const string Root = ArtRoot + "/Environment/Props/Basket";

                public const string Sprite = Root + "/2dbucket.png";

                public const string Material = Root + "/MAT_Basket.mat";
            }

            public static class Fairway
            {
                public const string Root = ArtRoot + "/Environment/Course/Fairway";

                public const string Albedo = Root + "/TEX_Fairway_Albedo.png";

                public const string Material = Root + "/MAT_Fairway.mat";
            }

            public static class Rough
            {
                public const string Root = ArtRoot + "/Environment/Course/Rough";

                public const string Albedo = Root + "/TEX_Rough_Albedo.png";

                public const string Material = Root + "/MAT_Rough.mat";
            }

            public static class Green
            {
                public const string Root = ArtRoot + "/Environment/Course/Green";

                public const string Albedo = Root + "/TEX_Green_Albedo.png";

                public const string Material = Root + "/MAT_Green.mat";
            }

            public static class Tee
            {
                public const string Root = ArtRoot + "/Environment/Course/Tee";

                public const string Material = Root + "/MAT_Tee.mat";
            }

            public static class Foliage
            {
                public const string SpritesRoot = ArtRoot + "/Environment/Foliage/Sprites";

                public const string Tree = SpritesRoot + "/Tree.png";

                public static string Sprite(string name) => $"{SpritesRoot}/{name}.png";
            }

            public static class Skybox
            {
                public const string Root = ArtRoot + "/Environment/Skybox";

                public const string Panoramic = Root + "/TEX_Sky_Panoramic.png";

                public const string Material = Root + "/MAT_PrototypeSkybox.mat";

                public const string OvercastMaterial = Root + "/MAT_Sky_Overcast.mat";
            }
        }

        public static class Gameplay
        {
            public const string DiscRoot = ArtRoot + "/Gameplay/Disc";

            public const string DiscMaterial = DiscRoot + "/MAT_DiscOrange.mat";

            public const string DiscMaterialBlue = DiscRoot + "/MAT_DiscBlue.mat";

            public const string DiscMaterialYellow = DiscRoot + "/MAT_DiscYellow.mat";

            public const string DiscMaterialRed = DiscRoot + "/MAT_DiscRed.mat";
        }

        public static class Ui
        {
            public const string Root = ArtRoot + "/UI";

            public const string ReferenceRoot = Root + "/Reference";

            public const string WindIcon = Root + "/windicon.png";

            public static string ScoreBanner(string fileName) => $"{Root}/{fileName}.png";

            public const string HappyCharacterPose = Root + "/Happy Character Pose.png";

            public const string SadCharacterPose = Root + "/Sad Character Pose.png";

            public const string HoleSummaryBackground = Root + "/ScoreSummary_Background1.png";

            public const string LegacyHoleSummaryBackground = ReferenceRoot + "/HoleSummaryScore.png";

            public static class DiscPreview
            {
                public const string Root = ArtRoot + "/UI/DiscPreview";

                public const string DefaultSprite = Root + "/TEX_Disc_Preview_Default.png";
            }
        }

        public static class Prefabs
        {
            public const string GameplayRoot = "Assets/Prefabs/Gameplay";

            public const string CourseRoot = "Assets/Prefabs/Course";

            public const string UiRoot = "Assets/Prefabs/UI";

            public const string GameplayCallouts = UiRoot + "/GameplayCallouts.prefab";

            public const string Disc = GameplayRoot + "/Disc.prefab";

            public const string Basket = GameplayRoot + "/Basket.prefab";

            public const string TeePad = CourseRoot + "/TeePad.prefab";
        }

        public static class Scenes
        {
            public const string PrototypeRoot = "Assets/Scenes/Prototype";

            public const string MenuRoot = "Assets/Scenes/Menu";

            public const string Intro = MenuRoot + "/Intro.unity";

            public const string MainMenu = MenuRoot + "/MainMenu.unity";

            public const string CharacterSelect = MenuRoot + "/CharacterSelect.unity";

            public const string CourseSelect = MenuRoot + "/CourseSelect.unity";

            public const string Settings = MenuRoot + "/Settings.unity";

            public const string CourseEditorHub = MenuRoot + "/CourseEditorHub.unity";

            public const string PrototypeFlat3 = PrototypeRoot + "/PrototypeFlat3.unity";

            public const string CourseEditor = PrototypeRoot + "/CourseEditor.unity";
        }

        public static class Runtime
        {
            /// <summary>Single Resources bootstrap asset referencing art outside Resources/.</summary>
            public const string GameplayArtCatalog = "Assets/Resources/GameplayArtCatalog.asset";
        }

        public static class Data
        {
            public const string Root = "Assets/Data";

            public const string ThemesRoot = Root + "/Themes";

            public const string CoursesRoot = Root + "/Courses";

            public const string DiscsRoot = Root + "/Discs";

            public static class Discs
            {
                public const string Putter = DiscsRoot + "/Putter.asset";

                public const string Midrange = DiscsRoot + "/Midrange.asset";

                public const string Fairway = DiscsRoot + "/Fairway.asset";

                public const string Distance = DiscsRoot + "/Distance.asset";
            }
        }

        public static class ThirdParty
        {
            public const string TextMeshProRoot = "Assets/ThirdParty/TextMesh Pro";

            public const string TmpSettings = TextMeshProRoot + "/Resources/TMP Settings.asset";

            public const string TmpResourcesRoot = TextMeshProRoot + "/Resources/Fonts & Materials";

            public const string PixelEmulatorSdf = TmpResourcesRoot + "/Pixel Emulator SDF.asset";

            /// <summary>Resources.Load key (any Resources/Fonts &amp; Materials folder).</summary>
            public const string PixelEmulatorSdfResource = "Fonts & Materials/Pixel Emulator SDF";
        }
    }
}
