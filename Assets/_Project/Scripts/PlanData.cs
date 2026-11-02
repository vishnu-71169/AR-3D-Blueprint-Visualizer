using System.Collections.Generic;
using UnityEngine;

namespace ARBlueprint
{
    /// <summary>Axis-aligned rectangle on the floor plan: centre (x, z) and size (x, z) in model metres.</summary>
    public struct PlanBox
    {
        public float cx, cz, sx, sz;

        public PlanBox(float cx, float cz, float sx, float sz)
        {
            this.cx = cx;
            this.cz = cz;
            this.sx = sx;
            this.sz = sz;
        }
    }

    public class PlanRoom
    {
        // Model metres per real metre is 1:5, so one model square metre is 25 real square metres.
        public const float RealAreaPerModelArea = 25f;

        public string name;
        public PlanBox box;
        public string fillMaterial;

        public float RealArea => box.sx * box.sz * RealAreaPerModelArea;
    }

    /// <summary>The 2D description of a floor: the blueprint is drawn from this data.</summary>
    public class PlanData
    {
        public readonly List<PlanRoom> rooms = new List<PlanRoom>();
        public readonly List<PlanBox> walls = new List<PlanBox>();
        public readonly List<PlanBox> doors = new List<PlanBox>();
        public readonly List<PlanBox> windows = new List<PlanBox>();

        public void AddRoom(string name, float xMin, float zMin, float xMax, float zMax, string fillMaterial)
        {
            rooms.Add(new PlanRoom
            {
                name = name,
                box = new PlanBox((xMin + xMax) * 0.5f, (zMin + zMax) * 0.5f, xMax - xMin, zMax - zMin),
                fillMaterial = fillMaterial
            });
        }

        public void AddWall(float cx, float cz, float sx, float sz) => walls.Add(new PlanBox(cx, cz, sx, sz));

        public void AddDoor(float cx, float cz, float sx, float sz) => doors.Add(new PlanBox(cx, cz, sx, sz));

        public void AddWindow(float cx, float cz, float sx, float sz) => windows.Add(new PlanBox(cx, cz, sx, sz));
    }
}
