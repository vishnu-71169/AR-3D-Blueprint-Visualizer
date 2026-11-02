using System.Collections.Generic;
using UnityEngine;

namespace ARBlueprint
{
    /// <summary>Loads the generated materials from Resources and tints the wall material on demand.</summary>
    public static class MaterialLibrary
    {
        // Size (in model metres) of one texture tile; used to give box UVs a constant physical scale.
        private static readonly Dictionary<string, float> TileByName = new Dictionary<string, float>
        {
            { "FloorOak", 0.55f }, { "FloorWalnut", 0.55f }, { "FloorCeramicTile", 0.5f }, { "FloorMarble", 0.9f },
            { "FloorCarpetWarm", 0.4f }, { "FloorCarpetBlue", 0.4f }, { "Concrete", 1.2f }, { "WallPlaster", 0.9f },
            { "InteriorPlaster", 0.9f }, { "TerraceDecking", 0.6f }, { "Lawn", 0.45f }, { "DoorWood", 0.5f },
            { "FurnitureWood", 0.5f }, { "Fabric", 0.3f }, { "StairAccent", 1.0f }
        };

        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        private static readonly Dictionary<Material, float> TileByMaterial = new Dictionary<Material, float>();
        private static readonly Dictionary<WallColorOption, Material> TintedWalls =
            new Dictionary<WallColorOption, Material>();

        public static Material Get(string key)
        {
            if (Cache.TryGetValue(key, out Material cached) && cached != null)
                return cached;

            Material material = Resources.Load<Material>("Materials/" + key);
            if (material == null)
                Debug.LogError($"Material '{key}' was not found in Resources/Materials.");

            Cache[key] = material;
            if (material != null)
                TileByMaterial[material] = TileByName.TryGetValue(key, out float tile) ? tile : 1f;
            return material;
        }

        public static float TileMeters(Material material)
        {
            return material != null && TileByMaterial.TryGetValue(material, out float tile) ? tile : 1f;
        }

        public static Color WallColor(WallColorOption option)
        {
            switch (option)
            {
                case WallColorOption.White: return new Color(0.98f, 0.98f, 0.98f);
                case WallColorOption.SkyBlue: return new Color(0.70f, 0.84f, 0.95f);
                case WallColorOption.SageGreen: return new Color(0.72f, 0.84f, 0.72f);
                case WallColorOption.Peach: return new Color(0.98f, 0.80f, 0.68f);
                case WallColorOption.Grey: return new Color(0.75f, 0.76f, 0.78f);
                default: return new Color(0.96f, 0.94f, 0.89f);
            }
        }

        public static string WallColorName(WallColorOption option)
        {
            switch (option)
            {
                case WallColorOption.White: return "White";
                case WallColorOption.SkyBlue: return "Sky blue";
                case WallColorOption.SageGreen: return "Sage green";
                case WallColorOption.Peach: return "Peach";
                case WallColorOption.Grey: return "Grey";
                default: return "Default";
            }
        }

        /// <summary>Returns the exterior or interior wall material for a colour option.</summary>
        public static Material Wall(WallColorOption option, bool interior)
        {
            if (option == WallColorOption.Default)
                return Get(interior ? "InteriorPlaster" : "WallPlaster");

            if (TintedWalls.TryGetValue(option, out Material cached) && cached != null)
                return cached;

            Material baseMaterial = Get("WallPlaster");
            var tinted = new Material(baseMaterial) { name = "WallPlaster_" + option };
            tinted.SetColor("_BaseColor", WallColor(option));
            TileByMaterial[tinted] = TileMeters(baseMaterial);
            TintedWalls[option] = tinted;
            return tinted;
        }
    }
}
