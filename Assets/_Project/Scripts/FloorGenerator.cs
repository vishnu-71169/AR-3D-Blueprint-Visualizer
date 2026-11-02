using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARBlueprint
{
    /// <summary>
    /// Builds a floor from a <see cref="FloorSpec"/>. The same plan data drives the 2D blueprint, so the
    /// blueprint and the 3D model of a floor always agree.
    /// </summary>
    public static class FloorGenerator
    {
        private const float WallThickness = 0.06f;

        private class Ctx
        {
            public Transform parent;
            public MeshOwner owner;
            public Material wall;
            public Material inner;
            public bool furniture;
            public readonly PlanData plan = new PlanData();
        }

        private class RoomRect
        {
            public string name;
            public float xMin, zMin, xMax, zMax;
            public float CenterX => (xMin + xMax) * 0.5f;
            public float CenterZ => (zMin + zMax) * 0.5f;
            public float Width => xMax - xMin;
            public float Depth => zMax - zMin;
        }

        // ------------------------------------------------------------------ entry point

        public static FloorView Build(FloorSpec spec, int index, Transform parent)
        {
            string title = $"Floor {index + 1} - {spec.LayoutName}";
            var root = new GameObject(title);
            root.transform.SetParent(parent, false);
            MeshOwner owner = root.AddComponent<MeshOwner>();
            FloorView view = root.AddComponent<FloorView>();

            var model = new GameObject("Model3D");
            model.transform.SetParent(root.transform, false);

            var ctx = new Ctx
            {
                parent = model.transform,
                owner = owner,
                wall = MaterialLibrary.Wall(spec.wallColor, false),
                inner = MaterialLibrary.Wall(spec.wallColor, true),
                furniture = spec.furniture
            };

            switch (spec.layout)
            {
                case FloorLayout.LivingArea: BuildLivingArea(ctx); break;
                case FloorLayout.Bedrooms: BuildBedrooms(ctx); break;
                case FloorLayout.Terrace: BuildTerrace(ctx); break;
                default: BuildCustom(ctx, spec); break;
            }

            var plan = new GameObject("Plan2D");
            plan.transform.SetParent(root.transform, false);
            plan.transform.localPosition = new Vector3(0f, 0.075f, 0f);
            BuildPlan(plan.transform, owner, ctx.plan, title);

            view.Setup(model.transform, plan.transform, title);
            return view;
        }

        // ------------------------------------------------------------------ small helpers

        private static GameObject Box(Ctx c, string name, Vector3 center, Vector3 size, string materialKey)
        {
            return BoxBuilder.Box(c.parent, c.owner, name, center, size, MaterialLibrary.Get(materialKey));
        }

        private static GameObject Box(Ctx c, string name, Vector3 center, Vector3 size, Material material)
        {
            return BoxBuilder.Box(c.parent, c.owner, name, center, size, material);
        }

        private static void Wall(Ctx c, string name, float cx, float cz, float sx, float sz, Material material,
            float height = 0.55f, float baseY = 0.025f)
        {
            Box(c, name, new Vector3(cx, baseY + height * 0.5f, cz), new Vector3(sx, height, sz), material);
            c.plan.AddWall(cx, cz, sx, sz);
        }

        private static string FloorKey(FlooringOption option, string roomType)
        {
            switch (option)
            {
                case FlooringOption.Wood: return "FloorOak";
                case FlooringOption.Tile: return "FloorCeramicTile";
                case FlooringOption.Carpet: return "FloorCarpetWarm";
                case FlooringOption.Marble: return "FloorMarble";
                default:
                    switch (roomType)
                    {
                        case "Kitchen": return "FloorCeramicTile";
                        case "Bedroom": return "FloorCarpetWarm";
                        case "Bathroom": return "FloorMarble";
                        default: return "FloorOak";
                    }
            }
        }

        private static string PlanFill(string floorKey)
        {
            switch (floorKey)
            {
                case "FloorOak":
                case "FloorWalnut": return "PlanFillWood";
                case "FloorCeramicTile": return "PlanFillTile";
                case "FloorCarpetWarm":
                case "FloorCarpetBlue": return "PlanFillCarpet";
                case "FloorMarble": return "PlanFillMarble";
                default: return "PlanFillOther";
            }
        }

        /// <summary>A window is a slim dark frame with a mullion and a reflective glass pane.</summary>
        private static void Window(Ctx c, string name, Vector3 center, Vector3 size)
        {
            bool alongX = size.x > size.z;
            float width = alongX ? size.x : size.z;
            float height = size.y;
            const float bar = 0.02f;
            float depth = (alongX ? size.z : size.x) * 1.6f;

            void Bar(string part, float along, float up, float lengthAlong, float lengthUp)
            {
                Vector3 position = alongX
                    ? new Vector3(center.x + along, center.y + up, center.z)
                    : new Vector3(center.x, center.y + up, center.z + along);
                Vector3 extent = alongX
                    ? new Vector3(lengthAlong, lengthUp, depth)
                    : new Vector3(depth, lengthUp, lengthAlong);
                Box(c, $"{name} {part}", position, extent, "WindowFrame");
            }

            Bar("Frame Top", 0f, height * 0.5f, width + bar * 2f, bar);
            Bar("Frame Bottom", 0f, -height * 0.5f, width + bar * 2f, bar);
            Bar("Frame Left", -width * 0.5f, 0f, bar, height);
            Bar("Frame Right", width * 0.5f, 0f, bar, height);
            Bar("Mullion", 0f, 0f, bar * 0.6f, height);

            Vector3 glassSize = alongX
                ? new Vector3(width, height, depth * 0.45f)
                : new Vector3(depth * 0.45f, height, width);
            Box(c, name + " Glass", center, glassSize, "WindowGlass");
            c.plan.AddWindow(center.x, center.z, size.x, size.z);
        }

        private static void Door(Ctx c, string name, float cx, float cz, float sx, float sz)
        {
            Box(c, name, new Vector3(cx, 0.20f, cz), new Vector3(sx, 0.38f, sz), "DoorWood");
            c.plan.AddDoor(cx, cz, sx, sz);
        }

        // ------------------------------------------------------------------ default floor 1: living area

        private static void OuterWallsDefault(Ctx c)
        {
            Wall(c, "Back Wall", 0f, 0.77f, 2.1f, 0.07f, c.wall);
            Wall(c, "Left Wall", -1.02f, 0f, 0.07f, 1.55f, c.wall);
            Wall(c, "Right Wall", 1.02f, 0f, 0.07f, 1.55f, c.wall);
            Wall(c, "Front Wall Left", -0.65f, -0.77f, 0.72f, 0.07f, c.wall);
            Wall(c, "Front Wall Middle", 0.08f, -0.77f, 0.34f, 0.07f, c.wall);
            Wall(c, "Front Wall Right", 0.72f, -0.77f, 0.58f, 0.07f, c.wall);
            Box(c, "Front Door", new Vector3(-0.22f, 0.20f, -0.79f), new Vector3(0.28f, 0.40f, 0.05f), "DoorWood");
            c.plan.AddDoor(-0.22f, -0.79f, 0.28f, 0.05f);
            Window(c, "Front Window", new Vector3(0.39f, 0.35f, -0.81f), new Vector3(0.28f, 0.22f, 0.035f));
            Window(c, "Back Window", new Vector3(-0.45f, 0.35f, 0.81f), new Vector3(0.42f, 0.22f, 0.035f));
            Window(c, "Side Window", new Vector3(1.06f, 0.35f, 0.28f), new Vector3(0.035f, 0.22f, 0.40f));
        }

        private static void BuildLivingArea(Ctx c)
        {
            Box(c, "Foundation", new Vector3(0f, 0.025f, 0f), new Vector3(2.1f, 0.05f, 1.6f), "Concrete");
            OuterWallsDefault(c);
            Wall(c, "Hall-Bedroom Wall", 0.25f, 0.12f, 0.06f, 1.00f, c.inner);
            Wall(c, "Kitchen Wall", -0.38f, -0.18f, 1.20f, 0.06f, c.inner);
            Door(c, "Interior Door 1", 0.25f, -0.53f, 0.07f, 0.28f);
            Door(c, "Interior Door 2", -0.72f, -0.18f, 0.28f, 0.07f);

            // Floor finishes: each room has its own flooring type.
            Box(c, "Living Room - Oak Wood", new Vector3(-0.35f, 0.056f, 0.32f), new Vector3(0.72f, 0.012f, 0.45f), "FloorOak");
            Box(c, "Kitchen - Ceramic Tile", new Vector3(-0.42f, 0.056f, -0.45f), new Vector3(0.60f, 0.012f, 0.38f), "FloorCeramicTile");
            Box(c, "Bedroom - Warm Carpet", new Vector3(0.62f, 0.056f, 0.05f), new Vector3(0.55f, 0.012f, 0.85f), "FloorCarpetWarm");

            c.plan.AddRoom("Living Room", -1.00f, -0.15f, 0.22f, 0.74f, "PlanFillWood");
            c.plan.AddRoom("Kitchen", -1.00f, -0.74f, 0.22f, -0.21f, "PlanFillTile");
            c.plan.AddRoom("Bedroom", 0.28f, -0.35f, 1.00f, 0.74f, "PlanFillCarpet");
            c.plan.AddRoom("Hall", 0.28f, -0.74f, 1.00f, -0.41f, "PlanFillOther");

            if (!c.furniture)
                return;

            Box(c, "Sofa", new Vector3(-0.45f, 0.10f, 0.58f), new Vector3(0.50f, 0.09f, 0.16f), "Fabric");
            Box(c, "Sofa Back", new Vector3(-0.45f, 0.15f, 0.65f), new Vector3(0.50f, 0.10f, 0.03f), "Fabric");
            Box(c, "Coffee Table", new Vector3(-0.45f, 0.095f, 0.34f), new Vector3(0.20f, 0.04f, 0.12f), "FurnitureWood");
            Box(c, "Kitchen Counter", new Vector3(-0.42f, 0.12f, -0.70f), new Vector3(0.80f, 0.12f, 0.10f), "FloorMarble");
            Box(c, "Bed", new Vector3(0.70f, 0.11f, 0.35f), new Vector3(0.34f, 0.07f, 0.46f), "Fabric");
            Box(c, "Pillow", new Vector3(0.70f, 0.16f, 0.52f), new Vector3(0.22f, 0.03f, 0.08f), "WallPlaster");
        }

        // ------------------------------------------------------------------ default floor 2: bedrooms

        private static void BuildBedrooms(Ctx c)
        {
            Box(c, "Second Floor Base", new Vector3(0f, 0.025f, 0f), new Vector3(2.1f, 0.05f, 1.6f), "Concrete");
            OuterWallsDefault(c);
            Wall(c, "Bedroom Divider", 0f, 0.12f, 0.06f, 1.00f, c.inner);
            Wall(c, "Bathroom Divider", 0.57f, -0.24f, 0.78f, 0.06f, c.inner);
            Box(c, "Stair Opening Edge", new Vector3(-0.55f, 0.12f, -0.30f), new Vector3(0.55f, 0.18f, 0.06f), "StairAccent");
            Door(c, "Bedroom Door", 0f, -0.48f, 0.07f, 0.30f);
            Door(c, "Bathroom Door", 0.53f, -0.24f, 0.28f, 0.07f);

            Box(c, "Bedroom A - Walnut Wood", new Vector3(-0.48f, 0.056f, 0.20f), new Vector3(0.72f, 0.012f, 0.76f), "FloorWalnut");
            Box(c, "Bedroom B - Blue Carpet", new Vector3(0.48f, 0.056f, 0.26f), new Vector3(0.70f, 0.012f, 0.62f), "FloorCarpetBlue");
            Box(c, "Bathroom - Marble", new Vector3(0.60f, 0.056f, -0.48f), new Vector3(0.58f, 0.012f, 0.32f), "FloorMarble");

            c.plan.AddRoom("Bedroom A", -1.00f, -0.74f, -0.03f, 0.74f, "PlanFillWood");
            c.plan.AddRoom("Bedroom B", 0.03f, -0.21f, 1.00f, 0.74f, "PlanFillCarpet");
            c.plan.AddRoom("Bathroom", 0.18f, -0.74f, 1.00f, -0.27f, "PlanFillMarble");

            if (!c.furniture)
                return;

            Box(c, "Bed A", new Vector3(-0.55f, 0.11f, 0.45f), new Vector3(0.40f, 0.07f, 0.50f), "Fabric");
            Box(c, "Pillow A", new Vector3(-0.55f, 0.16f, 0.64f), new Vector3(0.26f, 0.03f, 0.08f), "WallPlaster");
            Box(c, "Wardrobe A", new Vector3(-0.90f, 0.20f, 0.10f), new Vector3(0.10f, 0.30f, 0.40f), "FurnitureWood");
            Box(c, "Bed B", new Vector3(0.55f, 0.11f, 0.42f), new Vector3(0.40f, 0.07f, 0.50f), "Fabric");
            Box(c, "Pillow B", new Vector3(0.55f, 0.16f, 0.61f), new Vector3(0.26f, 0.03f, 0.08f), "WallPlaster");
            Box(c, "Bathtub", new Vector3(0.82f, 0.10f, -0.50f), new Vector3(0.30f, 0.09f, 0.18f), "WallPlaster");
        }

        // ------------------------------------------------------------------ default floor 3: rooftop terrace

        private static void BuildTerrace(Ctx c)
        {
            Box(c, "Roof Slab", new Vector3(0f, 0.025f, 0f), new Vector3(2.1f, 0.05f, 1.6f), "Concrete");
            Box(c, "Terrace - Wood Decking", new Vector3(-0.40f, 0.056f, -0.02f), new Vector3(1.10f, 0.012f, 1.36f), "TerraceDecking");
            Box(c, "Terrace - Lawn", new Vector3(0.65f, 0.056f, -0.02f), new Vector3(0.80f, 0.012f, 1.36f), "Lawn");

            // Parapet walls with glass railing and metal posts.
            Wall(c, "Parapet Back", 0f, 0.775f, 2.1f, 0.05f, c.wall, 0.14f, 0.05f);
            Wall(c, "Parapet Front", 0f, -0.775f, 2.1f, 0.05f, c.wall, 0.14f, 0.05f);
            Wall(c, "Parapet Left", -1.025f, 0f, 0.05f, 1.5f, c.wall, 0.14f, 0.05f);
            Wall(c, "Parapet Right", 1.025f, 0f, 0.05f, 1.5f, c.wall, 0.14f, 0.05f);
            Box(c, "Railing Glass Back", new Vector3(0f, 0.255f, 0.775f), new Vector3(2.0f, 0.11f, 0.012f), "WindowGlass");
            Box(c, "Railing Glass Front", new Vector3(0f, 0.255f, -0.775f), new Vector3(2.0f, 0.11f, 0.012f), "WindowGlass");
            Box(c, "Railing Glass Left", new Vector3(-1.025f, 0.255f, 0f), new Vector3(0.012f, 0.11f, 1.5f), "WindowGlass");
            Box(c, "Railing Glass Right", new Vector3(1.025f, 0.255f, 0f), new Vector3(0.012f, 0.11f, 1.5f), "WindowGlass");
            foreach (Vector2 corner in new[] { new Vector2(-1.025f, 0.775f), new Vector2(1.025f, 0.775f),
                         new Vector2(-1.025f, -0.775f), new Vector2(1.025f, -0.775f) })
                Box(c, "Railing Post", new Vector3(corner.x, 0.20f, corner.y), new Vector3(0.025f, 0.26f, 0.025f), "BrushedMetal");

            // Stair housing leading down to the lower floors.
            Box(c, "Stair Housing", new Vector3(-0.80f, 0.19f, 0.50f), new Vector3(0.45f, 0.28f, 0.50f), c.wall);
            Box(c, "Stair Housing Roof", new Vector3(-0.80f, 0.345f, 0.50f), new Vector3(0.50f, 0.03f, 0.55f), "Concrete");
            Box(c, "Stair Door", new Vector3(-0.80f, 0.14f, 0.245f), new Vector3(0.16f, 0.18f, 0.012f), "DoorWood");

            c.plan.AddRoom("Deck", -0.95f, -0.70f, 0.15f, 0.66f, "PlanFillDeck");
            c.plan.AddRoom("Lawn", 0.25f, -0.70f, 1.00f, 0.66f, "PlanFillGrass");
            c.plan.AddRoom("Stair", -1.025f, 0.25f, -0.575f, 0.75f, "PlanFillOther");
            c.plan.AddDoor(-0.80f, 0.245f, 0.16f, 0.03f);

            if (!c.furniture)
                return;

            // Pergola with a seating corner.
            foreach (Vector2 post in new[] { new Vector2(-0.90f, -0.60f), new Vector2(-0.10f, -0.60f),
                         new Vector2(-0.90f, -0.05f), new Vector2(-0.10f, -0.05f) })
                Box(c, "Pergola Post", new Vector3(post.x, 0.235f, post.y), new Vector3(0.03f, 0.36f, 0.03f), "FurnitureWood");
            Box(c, "Pergola Beam Front", new Vector3(-0.50f, 0.42f, -0.60f), new Vector3(0.95f, 0.025f, 0.035f), "FurnitureWood");
            Box(c, "Pergola Beam Back", new Vector3(-0.50f, 0.42f, -0.05f), new Vector3(0.95f, 0.025f, 0.035f), "FurnitureWood");
            for (int i = 0; i < 6; i++)
                Box(c, "Pergola Slat", new Vector3(-0.86f + i * 0.145f, 0.44f, -0.325f), new Vector3(0.02f, 0.015f, 0.62f), "FurnitureWood");
            Box(c, "Outdoor Sofa", new Vector3(-0.50f, 0.105f, -0.52f), new Vector3(0.60f, 0.08f, 0.13f), "Fabric");
            Box(c, "Outdoor Sofa Back", new Vector3(-0.50f, 0.15f, -0.575f), new Vector3(0.60f, 0.09f, 0.03f), "Fabric");
            Box(c, "Outdoor Table", new Vector3(-0.50f, 0.095f, -0.26f), new Vector3(0.20f, 0.05f, 0.20f), "FloorMarble");

            foreach (float z in new[] { 0.58f, -0.52f })
            {
                Box(c, "Planter", new Vector3(0.65f, 0.115f, z), new Vector3(0.32f, 0.12f, 0.20f), "Concrete");
                Box(c, "Planter Soil", new Vector3(0.65f, 0.178f, z), new Vector3(0.28f, 0.012f, 0.16f), "Lawn");
                BoxBuilder.Sphere(c.parent, "Bush", new Vector3(0.58f, 0.23f, z), 0.13f, MaterialLibrary.Get("Lawn"));
                BoxBuilder.Sphere(c.parent, "Bush", new Vector3(0.74f, 0.22f, z), 0.11f, MaterialLibrary.Get("Lawn"));
            }

            Box(c, "Tree Trunk", new Vector3(0.65f, 0.17f, 0.03f), new Vector3(0.035f, 0.22f, 0.035f), "FurnitureWood");
            BoxBuilder.Sphere(c.parent, "Tree Canopy", new Vector3(0.65f, 0.34f, 0.03f), 0.22f, MaterialLibrary.Get("Lawn"));
        }

        // ------------------------------------------------------------------ custom floor

        private static void BuildCustom(Ctx c, FloorSpec spec)
        {
            const float x0 = -0.99f, x1 = 0.99f, z0 = -0.735f, z1 = 0.735f, gap = 0.03f;
            int n = Mathf.Clamp(spec.roomCount, 1, 4);

            Box(c, "Floor Slab", new Vector3(0f, 0.025f, 0f), new Vector3(2.1f, 0.05f, 1.6f), "Concrete");
            Wall(c, "Back Wall", 0f, 0.77f, 2.1f, 0.07f, c.wall);
            Wall(c, "Front Wall", 0f, -0.77f, 2.1f, 0.07f, c.wall);
            Wall(c, "Left Wall", -1.02f, 0f, 0.07f, 1.55f, c.wall);
            Wall(c, "Right Wall", 1.02f, 0f, 0.07f, 1.55f, c.wall);

            string[] names;
            var rooms = new List<RoomRect>();
            switch (n)
            {
                case 1:
                    names = new[] { "Living Room" };
                    rooms.Add(new RoomRect { xMin = x0, zMin = z0, xMax = x1, zMax = z1 });
                    break;
                case 2:
                    names = new[] { "Living Room", "Bedroom" };
                    rooms.Add(new RoomRect { xMin = x0, zMin = z0, xMax = -gap, zMax = z1 });
                    rooms.Add(new RoomRect { xMin = gap, zMin = z0, xMax = x1, zMax = z1 });
                    break;
                case 3:
                    names = new[] { "Living Room", "Kitchen", "Bedroom" };
                    rooms.Add(new RoomRect { xMin = x0, zMin = z0, xMax = -gap, zMax = z1 });
                    rooms.Add(new RoomRect { xMin = gap, zMin = z0, xMax = x1, zMax = -gap });
                    rooms.Add(new RoomRect { xMin = gap, zMin = gap, xMax = x1, zMax = z1 });
                    break;
                default:
                    names = new[] { "Living Room", "Kitchen", "Bedroom", "Bathroom" };
                    rooms.Add(new RoomRect { xMin = x0, zMin = gap, xMax = -gap, zMax = z1 });
                    rooms.Add(new RoomRect { xMin = x0, zMin = z0, xMax = -gap, zMax = -gap });
                    rooms.Add(new RoomRect { xMin = gap, zMin = gap, xMax = x1, zMax = z1 });
                    rooms.Add(new RoomRect { xMin = gap, zMin = z0, xMax = x1, zMax = -gap });
                    break;
            }

            for (int i = 0; i < rooms.Count; i++)
                rooms[i].name = names[i];

            // Partition walls with doorways.
            if (n >= 2)
            {
                float[] gaps = n == 2 ? new[] { 0f } : new[] { -0.37f, 0.37f };
                Partition(c, true, 0f, z0, z1, gaps);
            }

            if (n == 3)
                Partition(c, false, 0f, 0f, x1, new[] { 0.5f });
            else if (n == 4)
                Partition(c, false, 0f, x0, x1, new[] { -0.5f, 0.5f });

            // Entrance door on the front wall, windows on the other exterior sides.
            RoomRect entrance = rooms[0];
            foreach (RoomRect room in rooms)
            {
                if (room.zMin <= z0 + 0.001f && room.xMin < entrance.xMin + 0.001f && Mathf.Approximately(room.zMin, z0))
                    entrance = room;
            }

            foreach (RoomRect room in rooms)
            {
                if (Mathf.Approximately(room.zMax, z1))
                    Window(c, $"{room.name} Back Window", new Vector3(room.CenterX, 0.35f, 0.81f),
                        new Vector3(Mathf.Min(0.42f, room.Width * 0.5f), 0.22f, 0.035f));

                if (Mathf.Approximately(room.zMin, z0))
                {
                    if (room == entrance)
                    {
                        Box(c, "Front Door", new Vector3(room.CenterX, 0.20f, -0.79f), new Vector3(0.28f, 0.40f, 0.05f), "DoorWood");
                        c.plan.AddDoor(room.CenterX, -0.79f, 0.28f, 0.05f);
                    }
                    else
                    {
                        Window(c, $"{room.name} Front Window", new Vector3(room.CenterX, 0.35f, -0.81f),
                            new Vector3(Mathf.Min(0.36f, room.Width * 0.5f), 0.22f, 0.035f));
                    }
                }

                if (Mathf.Approximately(room.xMin, x0))
                    Window(c, $"{room.name} Left Window", new Vector3(-1.06f, 0.35f, room.CenterZ),
                        new Vector3(0.035f, 0.22f, Mathf.Min(0.40f, room.Depth * 0.5f)));

                if (Mathf.Approximately(room.xMax, x1))
                    Window(c, $"{room.name} Right Window", new Vector3(1.06f, 0.35f, room.CenterZ),
                        new Vector3(0.035f, 0.22f, Mathf.Min(0.40f, room.Depth * 0.5f)));
            }

            // Flooring, plan rooms, and furniture for every room.
            foreach (RoomRect room in rooms)
            {
                string floorKey = FloorKey(spec.flooring, room.name);
                Box(c, $"{room.name} - Flooring", new Vector3(room.CenterX, 0.056f, room.CenterZ),
                    new Vector3(room.Width, 0.012f, room.Depth), floorKey);
                c.plan.AddRoom(room.name, room.xMin, room.zMin, room.xMax, room.zMax, PlanFill(floorKey));

                if (c.furniture)
                    Furnish(c, room);
            }
        }

        /// <summary>A wall along one axis with doorways; each doorway has a lintel and a door mark on the plan.</summary>
        private static void Partition(Ctx c, bool vertical, float fixedPos, float from, float to, float[] gaps)
        {
            const float doorWidth = 0.30f;
            float cursor = from;
            int index = 0;
            foreach (float gapCenter in gaps)
            {
                float gapStart = gapCenter - doorWidth * 0.5f;
                float gapEnd = gapCenter + doorWidth * 0.5f;
                PartitionSegment(c, vertical, fixedPos, cursor, gapStart, index++);
                // Lintel over the doorway.
                float lintelHeight = 0.145f;
                float lintelY = 0.025f + 0.55f - lintelHeight * 0.5f;
                if (vertical)
                    Box(c, "Lintel", new Vector3(fixedPos, lintelY, gapCenter), new Vector3(WallThickness, lintelHeight, doorWidth), c.inner);
                else
                    Box(c, "Lintel", new Vector3(gapCenter, lintelY, fixedPos), new Vector3(doorWidth, lintelHeight, WallThickness), c.inner);

                if (vertical)
                    c.plan.AddDoor(fixedPos, gapCenter, 0.025f, doorWidth);
                else
                    c.plan.AddDoor(gapCenter, fixedPos, doorWidth, 0.025f);

                cursor = gapEnd;
            }

            PartitionSegment(c, vertical, fixedPos, cursor, to, index);
        }

        private static void PartitionSegment(Ctx c, bool vertical, float fixedPos, float from, float to, int index)
        {
            float length = to - from;
            if (length < 0.01f)
                return;

            float mid = (from + to) * 0.5f;
            if (vertical)
                Wall(c, $"Partition {index}", fixedPos, mid, WallThickness, length, c.inner);
            else
                Wall(c, $"Partition {index}", mid, fixedPos, length, WallThickness, c.inner);
        }

        private static void Furnish(Ctx c, RoomRect room)
        {
            float cx = room.CenterX, cz = room.CenterZ;
            switch (room.name)
            {
                case "Living Room":
                {
                    float sofa = Mathf.Min(0.5f, room.Width * 0.6f);
                    Box(c, "Sofa", new Vector3(cx, 0.10f, room.zMax - 0.12f), new Vector3(sofa, 0.09f, 0.16f), "Fabric");
                    Box(c, "Sofa Back", new Vector3(cx, 0.15f, room.zMax - 0.05f), new Vector3(sofa, 0.10f, 0.03f), "Fabric");
                    Box(c, "Coffee Table", new Vector3(cx, 0.095f, cz - 0.05f), new Vector3(0.20f, 0.04f, 0.12f), "FurnitureWood");
                    break;
                }
                case "Kitchen":
                {
                    float counter = Mathf.Min(0.8f, room.Width * 0.8f);
                    Box(c, "Kitchen Counter", new Vector3(cx, 0.12f, room.zMin + 0.08f), new Vector3(counter, 0.12f, 0.10f), "FloorMarble");
                    Box(c, "Dining Table", new Vector3(cx, 0.095f, cz + 0.10f), new Vector3(0.24f, 0.05f, 0.18f), "FurnitureWood");
                    break;
                }
                case "Bedroom":
                    Box(c, "Bed", new Vector3(cx, 0.11f, cz + 0.05f), new Vector3(0.34f, 0.07f, 0.46f), "Fabric");
                    Box(c, "Pillow", new Vector3(cx, 0.16f, cz + 0.22f), new Vector3(0.22f, 0.03f, 0.08f), "WallPlaster");
                    break;
                case "Bathroom":
                    Box(c, "Bathtub", new Vector3(room.xMax - 0.2f, 0.10f, room.zMin + 0.14f), new Vector3(0.30f, 0.09f, 0.18f), "WallPlaster");
                    break;
            }
        }

        // ------------------------------------------------------------------ 2D blueprint

        private static void Flat(Transform parent, MeshOwner owner, string name, PlanBox box, float baseY, float height,
            string materialKey)
        {
            BoxBuilder.Box(parent, owner, name, new Vector3(box.cx, baseY + height * 0.5f, box.cz),
                new Vector3(box.sx, height, box.sz), MaterialLibrary.Get(materialKey));
        }

        private static void BuildPlan(Transform root, MeshOwner owner, PlanData plan, string title)
        {
            Flat(root, owner, "Paper", new PlanBox(0f, 0f, 2.3f, 1.9f), 0f, 0.004f, "PlanPaper");

            foreach (PlanRoom room in plan.rooms)
                Flat(root, owner, "Room " + room.name, room.box, 0.004f, 0.003f, room.fillMaterial);

            foreach (PlanBox wall in plan.walls)
                Flat(root, owner, "Wall", wall, 0.007f, 0.006f, "PlanWall");

            foreach (PlanBox door in plan.doors)
                Flat(root, owner, "Door", door, 0.013f, 0.003f, "PlanDoor");

            foreach (PlanBox window in plan.windows)
                Flat(root, owner, "Window", window, 0.013f, 0.003f, "PlanWindow");

            // Labels on a flat world-space canvas: top of the text points to the back of the building.
            var canvasObject = new GameObject("Labels", typeof(Canvas));
            canvasObject.transform.SetParent(root, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2400f, 2000f);
            canvasRect.localScale = Vector3.one * 0.001f;
            canvasRect.localRotation = Quaternion.Euler(90f, 0f, 0f);
            canvasRect.localPosition = new Vector3(0f, 0.02f, 0f);

            foreach (PlanRoom room in plan.rooms)
            {
                string text = $"{room.name}\n{room.RealArea:0.#} m²";
                AddLabel(canvasRect, text, room.box.cx, room.box.cz, 50, FontStyle.Normal);
            }

            AddLabel(canvasRect, title.ToUpperInvariant(), 0f, 0.885f, 62, FontStyle.Bold);
            AddLabel(canvasRect, "Plan scale 1:5   |   10.5 m x 8.0 m", 0f, -0.885f, 46, FontStyle.Italic);
        }

        private static void AddLabel(RectTransform parent, string text, float x, float z, int fontSize, FontStyle style)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1500f, 220f);
            rect.anchoredPosition = new Vector2(x * 1000f, z * 1000f);

            Text label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.05f, 0.08f, 0.20f);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
        }
    }
}
