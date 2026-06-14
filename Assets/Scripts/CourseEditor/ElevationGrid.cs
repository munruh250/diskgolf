using System;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    [Serializable]
    public sealed class ElevationGrid
    {
        public int Width;
        public int Height;
        public float[] Heights = Array.Empty<float>();

        public ElevationGrid() { }

        public ElevationGrid(int width, int height)
        {
            EnsureSize(width, height);
        }

        public void EnsureSize(int width, int height)
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);

            if (Width == width && Height == height && Heights.Length == width * height)
            {
                return;
            }

            var next = new float[width * height];
            int copyWidth = Mathf.Min(Width, width);
            int copyHeight = Mathf.Min(Height, height);

            for (int y = 0; y < copyHeight; y++)
            {
                for (int x = 0; x < copyWidth; x++)
                {
                    next[y * width + x] = Get(x, y);
                }
            }

            Width = width;
            Height = height;
            Heights = next;
        }

        public float Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height || Heights == null || Heights.Length == 0)
            {
                return 0f;
            }

            return Heights[y * Width + x];
        }

        public void Set(int x, int y, float height)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height || Heights == null || Heights.Length == 0)
            {
                return;
            }

            Heights[y * Width + x] = height;
        }

        /// <summary>Bilinear sample across the grid. <paramref name="u"/> and <paramref name="v"/> are normalized [0, 1].</summary>
        public float SampleBilinear(float u, float v)
        {
            if (Heights == null || Heights.Length == 0 || Width <= 0 || Height <= 0)
            {
                return 0f;
            }

            if (Width == 1 && Height == 1)
            {
                return Heights[0];
            }

            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);

            float x = u * (Width - 1);
            float y = v * (Height - 1);

            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = Mathf.Min(x0 + 1, Width - 1);
            int y1 = Mathf.Min(y0 + 1, Height - 1);

            float tx = x - x0;
            float ty = y - y0;

            float h00 = Get(x0, y0);
            float h10 = Get(x1, y0);
            float h01 = Get(x0, y1);
            float h11 = Get(x1, y1);

            float top = Mathf.Lerp(h00, h10, tx);
            float bottom = Mathf.Lerp(h01, h11, tx);
            return Mathf.Lerp(top, bottom, ty);
        }
    }
}
