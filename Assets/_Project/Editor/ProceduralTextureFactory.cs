#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ARBlueprintEditor
{
    /// <summary>
    /// Generates tileable albedo and normal textures for the building materials, so the project needs
    /// no external art assets. Every texture wraps seamlessly.
    /// </summary>
    public static class ProceduralTextureFactory
    {
        private const int Size = 512;

        public class Result
        {
            public Texture2D Albedo;
            public Texture2D Normal;
        }

        // ---------------- public recipes ----------------

        public static Result WoodPlanks(string folder, string name, int rows, Color dark, Color light, int seed)
        {
            return Build(folder, name, 2.2f, (u, v) =>
            {
                float rv = v * rows;
                int row = Mathf.FloorToInt(rv);
                float fv = rv - row;
                float offset = Hash(row, 0, seed) > 0.5f ? 0.5f : 0f;
                float s = u * 2f + offset;
                int plank = Mathf.FloorToInt(s);
                float fu = s - plank;
                float tone = Mathf.Lerp(0.55f, 1f, Hash(row, Mod(plank, 2), seed + 7));
                float grain = VNoise(u * 4f, v * 70f, 4, 70, seed + 3) * 0.6f
                              + VNoise(u * 12f, v * 140f, 12, 140, seed + 5) * 0.4f;
                bool seam = fv < 0.035f || fv > 0.965f || fu < 0.006f || fu > 0.994f;
                Color c = Color.Lerp(dark, light, Mathf.Clamp01(tone * 0.55f + grain * 0.45f));
                if (seam)
                    return (c * 0.35f, 0.15f);
                return (c, 0.65f + grain * 0.25f);
            });
        }

        public static Result CeramicTile(string folder, string name, int tiles, Color baseColor, Color grout, int seed)
        {
            return Build(folder, name, 2.5f, (u, v) =>
            {
                float tu = u * tiles;
                float tv = v * tiles;
                int ix = Mathf.FloorToInt(tu);
                int iy = Mathf.FloorToInt(tv);
                float fu = tu - ix;
                float fv = tv - iy;
                bool isGrout = fu < 0.025f || fu > 0.975f || fv < 0.025f || fv > 0.975f;
                if (isGrout)
                    return (grout, 0.2f);
                float speck = Fbm(u, v, 24, 3, seed) * 0.08f;
                float shade = (Hash(Mod(ix, tiles), Mod(iy, tiles), seed) - 0.5f) * 0.07f;
                return (baseColor * (0.97f + speck + shade), 0.85f);
            });
        }

        public static Result Marble(string folder, string name, Color baseColor, Color vein, int seed)
        {
            return Build(folder, name, 0.6f, (u, v) =>
            {
                float warp = Fbm(u, v, 3, 5, seed) * 1.6f;
                float t = (u * 2f + v * 1f) + warp;
                float vein1 = Mathf.Pow(1f - Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)), 14f);
                float warp2 = Fbm(u, v, 5, 4, seed + 11) * 1.2f;
                float vein2 = Mathf.Pow(1f - Mathf.Abs(Mathf.Sin((u * 1f - v * 3f + warp2) * Mathf.PI * 2f)), 22f);
                float cloud = Fbm(u, v, 6, 4, seed + 21) * 0.08f;
                Color c = Color.Lerp(baseColor, vein, Mathf.Clamp01(vein1 * 0.75f + vein2 * 0.45f));
                return (c * (0.96f + cloud), 0.5f + cloud);
            });
        }

        public static Result Carpet(string folder, string name, Color baseColor, int seed)
        {
            return Build(folder, name, 4f, (u, v) =>
            {
                float fine = VNoise(u * 160f, v * 160f, 160, 160, seed);
                float soft = Fbm(u, v, 6, 3, seed + 2);
                return (baseColor * (0.72f + fine * 0.4f + soft * 0.12f), 0.35f + fine * 0.65f);
            });
        }

        public static Result Concrete(string folder, string name, Color baseColor, int seed)
        {
            return Build(folder, name, 1.2f, (u, v) =>
            {
                float n = Fbm(u, v, 8, 5, seed);
                float pit = Hash(Mathf.FloorToInt(u * Size), Mathf.FloorToInt(v * Size), seed + 4);
                float speck = pit > 0.992f ? 0.55f : 1f;
                return (baseColor * (0.78f + n * 0.42f) * speck, n);
            });
        }

        public static Result Plaster(string folder, string name, Color baseColor, int seed)
        {
            return Build(folder, name, 0.7f, (u, v) =>
            {
                float n = Fbm(u, v, 6, 4, seed);
                float fine = VNoise(u * 96f, v * 96f, 96, 96, seed + 1);
                return (baseColor * (0.93f + n * 0.10f + fine * 0.02f), n * 0.6f + fine * 0.4f);
            });
        }

        public static Result Decking(string folder, string name, int rows, Color baseColor, int seed)
        {
            return Build(folder, name, 2f, (u, v) =>
            {
                float rv = v * rows;
                int row = Mathf.FloorToInt(rv);
                float fv = rv - row;
                float tone = Mathf.Lerp(0.8f, 1.1f, Hash(row, 3, seed));
                float grain = VNoise(u * 3f, v * 60f, 3, 60, seed + 2) * 0.5f
                              + VNoise(u * 9f, v * 150f, 9, 150, seed + 9) * 0.5f;
                bool gap = fv < 0.06f || fv > 0.94f;
                Color c = baseColor * (tone * (0.82f + grain * 0.34f));
                if (gap)
                    return (c * 0.25f, 0.05f);
                return (c, 0.6f + grain * 0.3f);
            });
        }

        public static Result Grass(string folder, string name, int seed)
        {
            return Build(folder, name, 3f, (u, v) =>
            {
                float blade = VNoise(u * 128f, v * 128f, 128, 128, seed);
                float patch = Fbm(u, v, 6, 4, seed + 5);
                Color c = Color.Lerp(new Color(0.15f, 0.36f, 0.10f), new Color(0.42f, 0.64f, 0.22f),
                    Mathf.Clamp01(blade * 0.55f + patch * 0.55f));
                return (c, blade);
            });
        }

        /// <summary>Soft blue grid used to draw detected AR planes. One texture tile equals one grid cell.</summary>
        public static Texture2D PlaneGrid(string folder, string name)
        {
            const int size = 256;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fu = x / (float)size;
                    float fv = y / (float)size;
                    float edge = Mathf.Min(Mathf.Min(fu, 1f - fu), Mathf.Min(fv, 1f - fv));
                    float line = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(edge / 0.03f));
                    float alpha = Mathf.Lerp(0.14f, 0.95f, line);
                    pixels[y * size + x] = new Color(0.30f, 0.80f, 1f, alpha);
                }
            }

            return SavePng(folder, name, size, pixels, TextureKind.AlphaColor);
        }

        // ---------------- internals ----------------

        private enum TextureKind { Color, Normal, AlphaColor }

        private delegate (Color color, float height) Sampler(float u, float v);

        private static Result Build(string folder, string name, float normalStrength, Sampler sampler)
        {
            var colors = new Color32[Size * Size];
            var heights = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    (Color color, float height) = sampler((x + 0.5f) / Size, (y + 0.5f) / Size);
                    colors[y * Size + x] = new Color(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g),
                        Mathf.Clamp01(color.b), 1f);
                    heights[y * Size + x] = height;
                }
            }

            var normals = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float left = heights[y * Size + (x - 1 + Size) % Size];
                    float right = heights[y * Size + (x + 1) % Size];
                    float down = heights[((y - 1 + Size) % Size) * Size + x];
                    float up = heights[((y + 1) % Size) * Size + x];
                    Vector3 n = new Vector3((left - right) * normalStrength, (down - up) * normalStrength, 1f).normalized;
                    normals[y * Size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }

            return new Result
            {
                Albedo = SavePng(folder, name, Size, colors, TextureKind.Color),
                Normal = SavePng(folder, name + "_N", Size, normals, TextureKind.Normal)
            };
        }

        private static Texture2D SavePng(string folder, string name, int size, Color32[] pixels, TextureKind kind)
        {
            string path = $"{folder}/{name}.png";
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, kind != TextureKind.Color && kind != TextureKind.AlphaColor);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = kind == TextureKind.Normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = kind != TextureKind.Normal;
            importer.alphaSource = kind == TextureKind.AlphaColor ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = kind == TextureKind.AlphaColor;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 8;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Tileable value noise: lattice coordinates wrap with the given periods.
        private static float VNoise(float x, float y, int periodX, int periodY, int seed)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int xa = Mod(x0, periodX), xb = Mod(x0 + 1, periodX);
            int ya = Mod(y0, periodY), yb = Mod(y0 + 1, periodY);
            float a = Mathf.Lerp(Hash(xa, ya, seed), Hash(xb, ya, seed), fx);
            float b = Mathf.Lerp(Hash(xa, yb, seed), Hash(xb, yb, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        private static float Fbm(float u, float v, int basePeriod, int octaves, int seed)
        {
            float sum = 0f, amplitude = 0.5f, total = 0f;
            int period = basePeriod;
            for (int o = 0; o < octaves; o++)
            {
                sum += amplitude * VNoise(u * period, v * period, period, period, seed + o * 17);
                total += amplitude;
                amplitude *= 0.5f;
                period *= 2;
            }

            return sum / total;
        }

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65535f;
            }
        }

        private static int Mod(int value, int period)
        {
            int r = value % period;
            return r < 0 ? r + period : r;
        }
    }
}
#endif
