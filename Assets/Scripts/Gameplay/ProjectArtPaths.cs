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

            public static class Tee
            {
                public const string Root = ArtRoot + "/Environment/Course/Tee";

                public const string Material = Root + "/MAT_Tee.mat";
            }

            public static class Foliage
            {
                public const string SourceRoot = ArtRoot + "/Environment/Foliage/Source";

                public const string Sheet = SourceRoot + "/FoliageSheet.png";

                public const string SpritesRoot = ArtRoot + "/Environment/Foliage/Sprites";

                public static string Sprite(string name) => $"{SpritesRoot}/{name}.png";
            }

            public static class Skybox
            {
                public const string Root = ArtRoot + "/Environment/Skybox";

                public const string Panoramic = Root + "/TEX_Sky_Panoramic.png";

                public const string Material = Root + "/MAT_PrototypeSkybox.mat";
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

            public const string PrototypeFlat3 = PrototypeRoot + "/PrototypeFlat3.unity";
        }

        public static class Runtime
        {
            /// <summary>Single Resources bootstrap asset referencing art outside Resources/.</summary>
            public const string GameplayArtCatalog = "Assets/Resources/GameplayArtCatalog.asset";
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
