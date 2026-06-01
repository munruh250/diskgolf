using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DiskGolf.Gameplay
{
    /// <summary>Resolves gameplay art from the Resources catalog with editor fallbacks.</summary>
    public static class RuntimeArt
    {
        static GameplayArtCatalog _catalog;

        public static GameplayArtCatalog Catalog
        {
            get
            {
                if (_catalog != null)
                    return _catalog;

                _catalog = Resources.Load<GameplayArtCatalog>("GameplayArtCatalog");

#if UNITY_EDITOR
                if (_catalog == null)
                    _catalog = AssetDatabase.LoadAssetAtPath<GameplayArtCatalog>(
                        ProjectArtPaths.Runtime.GameplayArtCatalog);
#endif

                return _catalog;
            }
        }

        public static Sprite LoadFoliageSprite(string spriteName)
        {
            var sprite = Catalog?.GetFoliageSprite(spriteName);
            if (sprite != null)
                return sprite;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(ProjectArtPaths.Environment.Foliage.Sprite(spriteName));
#else
            return null;
#endif
        }

        public static Sprite LoadBasketSprite()
        {
            if (Catalog?.basket != null)
                return Catalog.basket;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(ProjectArtPaths.Environment.Basket.Sprite);
#else
            return null;
#endif
        }

        public static Texture2D LoadBasketTexture()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ProjectArtPaths.Environment.Basket.Sprite);
#else
            return null;
#endif
        }

        public static Sprite LoadThrowerSprite()
        {
            if (Catalog?.thrower != null)
                return Catalog.thrower;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(ProjectArtPaths.Characters.ThrowerSprite);
#else
            return null;
#endif
        }

        public static Material LoadPrototypeSkyboxMaterial()
        {
            if (Catalog?.prototypeSkybox != null)
                return Catalog.prototypeSkybox;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Material>(ProjectArtPaths.Environment.Skybox.Material);
#else
            return null;
#endif
        }

        public static Sprite LoadDiscPreviewSprite()
        {
            if (Catalog?.discPreviewDefault != null)
                return Catalog.discPreviewDefault;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(ProjectArtPaths.Ui.DiscPreview.DefaultSprite);
#else
            return null;
#endif
        }

        public static Material LoadDiscMaterial()
        {
            if (Catalog?.discDefault != null)
                return Catalog.discDefault;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Material>(ProjectArtPaths.Gameplay.DiscMaterial);
#else
            return null;
#endif
        }
    }
}
