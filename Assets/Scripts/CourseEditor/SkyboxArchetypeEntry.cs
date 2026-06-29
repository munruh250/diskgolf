using System;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    [Serializable]
    public sealed class SkyboxArchetypeEntry
    {
        public string skyboxId = "sky_clear";
        public Material material;
        public Sprite preview;
    }
}
