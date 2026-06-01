using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Runtime references to gameplay sprites and materials (lives in Resources/).</summary>
    [CreateAssetMenu(fileName = "GameplayArtCatalog", menuName = "DiskGolf/Art/Gameplay Art Catalog")]
    public sealed class GameplayArtCatalog : ScriptableObject
    {
        public Sprite grassLightA;

        public Sprite grassDarkA;

        public Sprite grassLightB;

        public Sprite grassDarkB;

        public Sprite treeConical;

        public Sprite treeRound;

        public Sprite basket;

        public Sprite thrower;

        public Material prototypeSkybox;

        public Sprite discPreviewDefault;

        public Material discDefault;

        public Sprite GetFoliageSprite(string spriteName) =>
            spriteName switch
            {
                FoliageSprites.GrassLightA => grassLightA,
                FoliageSprites.GrassDarkA => grassDarkA,
                FoliageSprites.GrassLightB => grassLightB,
                FoliageSprites.GrassDarkB => grassDarkB,
                FoliageSprites.TreeConical => treeConical,
                FoliageSprites.TreeRound => treeRound,
                _ => null,
            };
    }
}
