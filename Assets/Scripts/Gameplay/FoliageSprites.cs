using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Loads the standalone tree sprite via <see cref="RuntimeArt"/> catalog.</summary>
    public static class FoliageSprites
    {
        public const string Tree = "Tree";

        public static Sprite LoadTree() => Load(Tree);

        public static Sprite Load(string spriteName) => RuntimeArt.LoadFoliageSprite(spriteName);
    }
}
