using System;
using System.Collections.Generic;

namespace ARBlueprint
{
    public enum FloorLayout { LivingArea, Bedrooms, Terrace, Custom }

    public enum WallColorOption { Default, White, SkyBlue, SageGreen, Peach, Grey }

    public enum FlooringOption { Mixed, Wood, Tile, Carpet, Marble }

    /// <summary>Everything the floor generator needs to build one floor (2D blueprint and 3D model).</summary>
    [Serializable]
    public class FloorSpec
    {
        public FloorLayout layout = FloorLayout.Custom;
        public int roomCount = 3;
        public WallColorOption wallColor = WallColorOption.Default;
        public FlooringOption flooring = FlooringOption.Mixed;
        public bool furniture = true;

        public FloorSpec Clone()
        {
            return (FloorSpec)MemberwiseClone();
        }

        public string LayoutName
        {
            get
            {
                switch (layout)
                {
                    case FloorLayout.LivingArea: return "Living Area";
                    case FloorLayout.Bedrooms: return "Bedrooms";
                    case FloorLayout.Terrace: return "Rooftop Terrace";
                    default: return roomCount == 1 ? "Custom (1 room)" : $"Custom ({roomCount} rooms)";
                }
            }
        }
    }

    /// <summary>Up to three floors, listed from the ground floor upwards.</summary>
    [Serializable]
    public class BuildingSpec
    {
        public const int MaxFloors = 3;

        public List<FloorSpec> floors = new List<FloorSpec>();

        public BuildingSpec Clone()
        {
            var copy = new BuildingSpec();
            foreach (FloorSpec floor in floors)
                copy.floors.Add(floor.Clone());
            return copy;
        }

        public static FloorSpec DefaultFloor(int index)
        {
            FloorLayout layout = index == 0 ? FloorLayout.LivingArea
                : index == 1 ? FloorLayout.Bedrooms
                : FloorLayout.Terrace;
            return new FloorSpec { layout = layout };
        }

        /// <summary>The three original floors: living area, bedrooms, and rooftop terrace.</summary>
        public static BuildingSpec CreateDefault()
        {
            var spec = new BuildingSpec();
            for (int i = 0; i < MaxFloors; i++)
                spec.floors.Add(DefaultFloor(i));
            return spec;
        }
    }

    /// <summary>The building that will be shown the next time one is placed or rebuilt.</summary>
    public static class BuildingConfig
    {
        public static BuildingSpec Current = BuildingSpec.CreateDefault();
    }
}
