using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Runtime references to gameplay sprites and materials (lives in Resources/).</summary>
    [CreateAssetMenu(fileName = "GameplayArtCatalog", menuName = "DiskGolf/Art/Gameplay Art Catalog")]
    public sealed class GameplayArtCatalog : ScriptableObject
    {
        public Sprite treeRound;

        public Sprite basket;

        public Sprite thrower;

        public Material prototypeSkybox;

        public Sprite discPreviewDefault;

        public Material discDefault;

        public Sprite GetFoliageSprite(string spriteName) =>
            spriteName == FoliageSprites.Tree ? treeRound : null;
    }
}
