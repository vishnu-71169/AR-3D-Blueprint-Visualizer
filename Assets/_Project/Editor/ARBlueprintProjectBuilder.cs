#if UNITY_EDITOR
using System.IO;
using ARBlueprint;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

namespace ARBlueprintEditor
{
    /// <summary>One-time editor utility that creates every project asset and reference.</summary>
    public static class ARBlueprintProjectBuilder
    {
        private const string Root = "Assets/_Project";
        private const string OldMaterials = Root + "/Materials";
        private const string ResourcesFolder = Root + "/Resources";
        private const string Materials = ResourcesFolder + "/Materials";
        private const string Textures = Root + "/Textures";
        private const string Prefabs = Root + "/Prefabs";
        private const string Scenes = Root + "/Scenes";

        [MenuItem("AR Blueprint/Build Complete Project")]
        public static void BuildCompleteProject()
        {
            EnsureFolders();
            Material plane = CreateMaterials();

            GameObject buildingPrefab = CreateBuildingPrefab();
            GameObject planePrefab = CreatePlanePrefab(plane);
            CreateARScene(buildingPrefab, planePrefab);
            ConfigureURPCameraBackground();
            ConfigureURPQuality();
            ConfigureAndroid();
            ConfigureARCoreLoader();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("AR Building Blueprint Visualizer project created successfully.");
        }

        [MenuItem("AR Blueprint/Build Android APK")]
        public static void BuildAndroidApk()
        {
            Directory.CreateDirectory("Builds");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { $"{Scenes}/ARBuildingVisualizer.unity" },
                locationPathName = "Builds/ARBuildingBlueprintVisualizer.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException(
                    $"Android build failed with {report.summary.totalErrors} error(s).");

            Debug.Log($"Android APK built successfully: {options.locationPathName} ({report.summary.totalSize} bytes)");
        }

        private static void EnsureFolders()
        {
            // Materials now live in Resources so the floors can be generated at run time.
            if (AssetDatabase.IsValidFolder(OldMaterials))
                AssetDatabase.DeleteAsset(OldMaterials);
            if (AssetDatabase.IsValidFolder(Root + "/Meshes"))
                AssetDatabase.DeleteAsset(Root + "/Meshes");

            Directory.CreateDirectory(Materials);
            Directory.CreateDirectory(Textures);
            Directory.CreateDirectory(Prefabs);
            Directory.CreateDirectory(Scenes);
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------ materials

        /// <summary>Creates every material the generated floors use. Returns the AR plane material.</summary>
        private static Material CreateMaterials()
        {
            var wood = ProceduralTextureFactory.WoodPlanks(Textures, "OakPlanks", 8,
                new Color(0.42f, 0.27f, 0.14f), new Color(0.78f, 0.57f, 0.34f), 11);
            var tile = ProceduralTextureFactory.CeramicTile(Textures, "CeramicTile", 4,
                new Color(0.90f, 0.89f, 0.85f), new Color(0.50f, 0.50f, 0.48f), 23);
            var marble = ProceduralTextureFactory.Marble(Textures, "Marble", new Color(0.94f, 0.94f, 0.92f),
                new Color(0.42f, 0.45f, 0.50f), 37);
            var carpet = ProceduralTextureFactory.Carpet(Textures, "Carpet", new Color(0.62f, 0.64f, 0.68f), 41);
            var concrete = ProceduralTextureFactory.Concrete(Textures, "Concrete", new Color(0.70f, 0.70f, 0.69f), 53);
            var plaster = ProceduralTextureFactory.Plaster(Textures, "Plaster", new Color(0.96f, 0.94f, 0.89f), 67);
            var decking = ProceduralTextureFactory.Decking(Textures, "Decking", 6, new Color(0.56f, 0.45f, 0.35f), 71);
            var grass = ProceduralTextureFactory.Grass(Textures, "Grass", 83);
            Texture2D grid = ProceduralTextureFactory.PlaneGrid(Textures, "PlaneGrid");

            CreateLit("FloorOak", Color.white, wood, 0.45f, 0f, 0.8f);
            CreateLit("FloorWalnut", new Color(0.58f, 0.40f, 0.30f), wood, 0.5f, 0f, 0.8f);
            CreateLit("FloorCeramicTile", Color.white, tile, 0.78f, 0f, 0.6f);
            CreateLit("FloorMarble", Color.white, marble, 0.88f, 0f, 0.3f);
            CreateLit("FloorCarpetWarm", new Color(0.78f, 0.62f, 0.55f), carpet, 0.08f, 0f, 1.3f);
            CreateLit("FloorCarpetBlue", new Color(0.50f, 0.62f, 0.80f), carpet, 0.08f, 0f, 1.3f);
            CreateLit("Concrete", Color.white, concrete, 0.22f, 0f, 0.9f);
            CreateLit("WallPlaster", new Color(0.96f, 0.94f, 0.89f), plaster, 0.18f, 0f, 0.5f);
            CreateLit("InteriorPlaster", new Color(0.70f, 0.84f, 0.92f), plaster, 0.18f, 0f, 0.5f);
            CreateLit("TerraceDecking", Color.white, decking, 0.30f, 0f, 0.9f);
            CreateLit("Lawn", Color.white, grass, 0.10f, 0f, 1.2f);
            CreateLit("DoorWood", new Color(0.55f, 0.32f, 0.17f), wood, 0.55f, 0f, 0.8f);
            CreateLit("FurnitureWood", new Color(0.85f, 0.70f, 0.52f), wood, 0.45f, 0f, 0.8f);
            CreateLit("Fabric", new Color(0.35f, 0.45f, 0.58f), carpet, 0.12f, 0f, 1.0f);
            CreateLit("StairAccent", new Color(0.82f, 0.66f, 0.34f), concrete, 0.35f, 0.1f, 0.6f);
            CreateLit("WindowFrame", new Color(0.12f, 0.13f, 0.15f), null, 0.55f, 0.85f, 1f);
            CreateLit("BrushedMetal", new Color(0.80f, 0.82f, 0.85f), null, 0.72f, 0.9f, 1f);
            CreateGlass("WindowGlass", new Color(0.62f, 0.83f, 0.95f, 0.28f));

            // 2D blueprint materials (flat, unlit).
            CreateUnlit("PlanPaper", new Color(0.93f, 0.96f, 1.00f));
            CreateUnlit("PlanWall", new Color(0.08f, 0.12f, 0.25f));
            CreateUnlit("PlanDoor", new Color(0.55f, 0.33f, 0.15f));
            CreateUnlit("PlanWindow", new Color(0.25f, 0.55f, 0.90f));
            CreateUnlit("PlanFillWood", new Color(0.86f, 0.74f, 0.55f));
            CreateUnlit("PlanFillTile", new Color(0.82f, 0.84f, 0.86f));
            CreateUnlit("PlanFillCarpet", new Color(0.80f, 0.70f, 0.78f));
            CreateUnlit("PlanFillMarble", new Color(0.95f, 0.95f, 0.97f));
            CreateUnlit("PlanFillGrass", new Color(0.65f, 0.82f, 0.55f));
            CreateUnlit("PlanFillDeck", new Color(0.78f, 0.68f, 0.58f));
            CreateUnlit("PlanFillOther", new Color(0.88f, 0.88f, 0.90f));

            return CreatePlaneMaterial("DetectedPlane", grid);
        }

        private static void ApplyTransparency(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", 5f);
            material.SetFloat("_DstBlend", 10f);
            material.SetFloat("_SrcBlendAlpha", 1f);
            material.SetFloat("_DstBlendAlpha", 10f);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static Material NewMaterial(string name, string shaderName)
        {
            string path = $"{Materials}/{name}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                AssetDatabase.DeleteAsset(path);

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                throw new System.InvalidOperationException($"Shader not found: {shaderName}");

            var material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material CreateLit(string name, Color tint, ProceduralTextureFactory.Result texture,
            float smoothness, float metallic, float normalScale)
        {
            Material material = NewMaterial(name, "Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_SpecularHighlights", 1f);
            material.SetFloat("_EnvironmentReflections", 1f);

            if (texture != null)
            {
                material.SetTexture("_BaseMap", texture.Albedo);
                material.SetTexture("_BumpMap", texture.Normal);
                material.SetFloat("_BumpScale", normalScale);
                material.EnableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateGlass(string name, Color tint)
        {
            Material material = NewMaterial(name, "Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", 0.96f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_SpecularHighlights", 1f);
            material.SetFloat("_EnvironmentReflections", 1f);
            ApplyTransparency(material);
            material.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateUnlit(string name, Color color)
        {
            Material material = NewMaterial(name, "Universal Render Pipeline/Unlit");
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreatePlaneMaterial(string name, Texture2D grid)
        {
            Material material = NewMaterial(name, "Universal Render Pipeline/Unlit");
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", grid);
            material.SetTextureScale("_BaseMap", new Vector2(4f, 4f));
            ApplyTransparency(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------------ prefabs

        /// <summary>The building prefab is an empty root: its floors are generated at run time.</summary>
        private static GameObject CreateBuildingPrefab()
        {
            GameObject root = new GameObject("Blueprint Building");
            root.AddComponent<ARObjectManipulator>();
            root.AddComponent<BuildingController>();

            string path = $"{Prefabs}/BlueprintBuilding.prefab";
            AssetDatabase.DeleteAsset(path);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreatePlanePrefab(Material material)
        {
            GameObject plane = new GameObject("Detected Plane Visual");
            plane.AddComponent<MeshFilter>();
            plane.AddComponent<MeshRenderer>().sharedMaterial = material;
            plane.AddComponent<ARPlaneMeshVisualizer>();
            string path = $"{Prefabs}/DetectedPlane.prefab";
            AssetDatabase.DeleteAsset(path);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(plane, path);
            Object.DestroyImmediate(plane);
            return prefab;
        }

        // ------------------------------------------------------------------ scene

        private static void CreateARScene(GameObject buildingPrefab, GameObject planePrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject sessionObject = new GameObject("AR Session");
            sessionObject.AddComponent<ARSession>();
            sessionObject.AddComponent<ARInputManager>();

            GameObject originObject = new GameObject("XR Origin");
            XROrigin origin = originObject.AddComponent<XROrigin>();
            GameObject cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(originObject.transform, false);
            GameObject cameraObject = new GameObject("AR Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(cameraOffset.transform, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            cameraObject.AddComponent<AudioListener>();
            ARCameraManager cameraManager = cameraObject.AddComponent<ARCameraManager>();
            cameraObject.AddComponent<ARCameraBackground>();
            origin.Camera = camera;
            origin.CameraFloorOffsetObject = cameraOffset;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;

            ARPlaneManager planeManager = originObject.AddComponent<ARPlaneManager>();
            planeManager.planePrefab = planePrefab;
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            ARRaycastManager raycastManager = originObject.AddComponent<ARRaycastManager>();

            // Lighting: warm key light with soft shadows plus a sky-based ambient and reflection source.
            Material skybox = CreateSkyboxMaterial();
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.9f;

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.90f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            ARLightEstimation lightEstimation = cameraObject.AddComponent<ARLightEstimation>();
            SerializedObject lightData = new SerializedObject(lightEstimation);
            lightData.FindProperty("cameraManager").objectReferenceValue = cameraManager;
            lightData.FindProperty("mainLight").objectReferenceValue = light;
            lightData.ApplyModifiedPropertiesWithoutUndo();

            GameObject placementObject = new GameObject("Building Placement System");
            ARPlacementManager placement = placementObject.AddComponent<ARPlacementManager>();

            UiRefs ui = CreateUI();

            SerializedObject placementData = new SerializedObject(placement);
            placementData.FindProperty("raycastManager").objectReferenceValue = raycastManager;
            placementData.FindProperty("planeManager").objectReferenceValue = planeManager;
            placementData.FindProperty("buildingPrefab").objectReferenceValue = buildingPrefab;
            placementData.FindProperty("uiManager").objectReferenceValue = ui.Manager;
            placementData.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject uiData = new SerializedObject(ui.Manager);
            uiData.FindProperty("placementManager").objectReferenceValue = placement;
            uiData.FindProperty("creatorPanel").objectReferenceValue = ui.Creator;
            uiData.FindProperty("instructionText").objectReferenceValue = ui.Instruction;
            uiData.FindProperty("statusText").objectReferenceValue = ui.Status;
            uiData.FindProperty("floor1Button").objectReferenceValue = ui.Floor1;
            uiData.FindProperty("floor2Button").objectReferenceValue = ui.Floor2;
            uiData.FindProperty("floor3Button").objectReferenceValue = ui.Floor3;
            uiData.FindProperty("stackedButton").objectReferenceValue = ui.Stacked;
            uiData.FindProperty("plan2DButton").objectReferenceValue = ui.Plan2D;
            uiData.FindProperty("convert3DButton").objectReferenceValue = ui.Convert3D;
            uiData.FindProperty("createButton").objectReferenceValue = ui.CreateFloors;
            uiData.FindProperty("defaultPlansButton").objectReferenceValue = ui.DefaultPlans;
            uiData.FindProperty("resetButton").objectReferenceValue = ui.Reset;
            uiData.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject creatorData = new SerializedObject(ui.Creator);
            creatorData.FindProperty("placementManager").objectReferenceValue = placement;
            creatorData.ApplyModifiedPropertiesWithoutUndo();

            string scenePath = $"{Scenes}/ARBuildingVisualizer.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        }

        private static Material CreateSkyboxMaterial()
        {
            const string path = Root + "/ARSky.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var material = new Material(Shader.Find("Skybox/Procedural")) { name = "ARSky" };
            material.SetFloat("_SunSize", 0.04f);
            material.SetFloat("_AtmosphereThickness", 0.9f);
            material.SetFloat("_Exposure", 1.25f);
            material.SetColor("_SkyTint", new Color(0.62f, 0.70f, 0.82f));
            material.SetColor("_GroundColor", new Color(0.46f, 0.43f, 0.40f));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ------------------------------------------------------------------ UI

        private class UiRefs
        {
            public UIManager Manager;
            public FloorCreatorPanel Creator;
            public Text Instruction, Status;
            public Button Floor1, Floor2, Floor3, Stacked, Plan2D, Convert3D, CreateFloors, DefaultPlans, Reset;
        }

        private static UiRefs CreateUI()
        {
            var refs = new UiRefs();
            Font font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasObject = new GameObject("Mobile UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            refs.Manager = canvasObject.AddComponent<UIManager>();
            refs.Creator = canvasObject.AddComponent<FloorCreatorPanel>();

            Image header = CreatePanel("Header", canvas.transform, new Color(0.04f, 0.08f, 0.13f, 0.90f),
                new Vector2(0f, 0.85f), new Vector2(1f, 1f));
            CreateText("Title", header.transform, "AR BUILDING BLUEPRINT VISUALIZER", font, 34, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.95f));
            refs.Instruction = CreateText("Placement Instruction", header.transform,
                "Move the phone slowly, then tap a detected surface", font, 25, FontStyle.Normal,
                TextAnchor.MiddleCenter, new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.58f));
            refs.Status = CreateText("Status", header.transform, "Searching for a horizontal plane...", font, 21,
                FontStyle.Italic, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.25f));
            refs.Status.color = new Color(0.40f, 0.84f, 1f);

            Image footer = CreatePanel("Controls", canvas.transform, new Color(0.04f, 0.08f, 0.13f, 0.92f),
                new Vector2(0f, 0f), new Vector2(1f, 0.30f));
            CreateText("Gesture Help", footer.transform,
                "1 finger: move  |  2 fingers: rotate + pinch to scale", font, 22, FontStyle.Normal,
                TextAnchor.MiddleCenter, new Vector2(0.03f, 0.87f), new Vector2(0.97f, 0.99f));

            // Row 1: floors.
            refs.Floor1 = CreateButton("Floor 1 Button", footer.transform, "FLOOR 1", font,
                new Vector2(0.03f, 0.63f), new Vector2(0.25f, 0.85f));
            refs.Floor2 = CreateButton("Floor 2 Button", footer.transform, "FLOOR 2", font,
                new Vector2(0.27f, 0.63f), new Vector2(0.49f, 0.85f));
            refs.Floor3 = CreateButton("Floor 3 Button", footer.transform, "FLOOR 3", font,
                new Vector2(0.51f, 0.63f), new Vector2(0.73f, 0.85f));
            refs.Stacked = CreateButton("Stacked Button", footer.transform, "STACKED", font,
                new Vector2(0.75f, 0.63f), new Vector2(0.97f, 0.85f));
            // Row 2: 2D blueprint or 3D model.
            refs.Plan2D = CreateButton("2D Blueprint Button", footer.transform, "2D BLUEPRINT", font,
                new Vector2(0.03f, 0.38f), new Vector2(0.49f, 0.60f));
            refs.Convert3D = CreateButton("Convert 3D Button", footer.transform, "CONVERT TO 3D", font,
                new Vector2(0.51f, 0.38f), new Vector2(0.97f, 0.60f));
            // Row 3: creator, default plans, reset.
            refs.CreateFloors = CreateButton("Create Floors Button", footer.transform, "CREATE FLOORS", font,
                new Vector2(0.03f, 0.13f), new Vector2(0.33f, 0.35f));
            refs.DefaultPlans = CreateButton("Default Plans Button", footer.transform, "DEFAULT PLANS", font,
                new Vector2(0.35f, 0.13f), new Vector2(0.65f, 0.35f));
            refs.Reset = CreateButton("Reset Button", footer.transform, "RESET", font,
                new Vector2(0.67f, 0.13f), new Vector2(0.97f, 0.35f));
            CreateText("Placement Hint", footer.transform,
                "Blue grid = detected real-world surface", font, 18, FontStyle.Italic,
                TextAnchor.MiddleCenter, new Vector2(0.04f, 0.0f), new Vector2(0.96f, 0.12f));

            CreateCreatorPanel(canvas.transform, font, refs.Creator);

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            _ = eventSystem;
            return refs;
        }

        /// <summary>The floor creator: a dimmed full-screen overlay with a panel of option buttons.</summary>
        private static void CreateCreatorPanel(Transform canvas, Font font, FloorCreatorPanel creator)
        {
            Image dim = CreatePanel("Floor Creator", canvas, new Color(0f, 0f, 0f, 0.55f), Vector2.zero, Vector2.one);
            Image panel = CreatePanel("Panel", dim.transform, new Color(0.06f, 0.10f, 0.16f, 0.98f),
                new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.88f));

            CreateText("Title", panel.transform, "FLOOR CREATOR", font, 34, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.91f), new Vector2(0.95f, 0.99f));

            Button floorCount = CreateButton("Floor Count", panel.transform, "NUMBER OF FLOORS: 3", font,
                new Vector2(0.05f, 0.81f), new Vector2(0.95f, 0.90f));
            Button tab1 = CreateButton("Tab 1", panel.transform, "FLOOR 1", font,
                new Vector2(0.05f, 0.71f), new Vector2(0.33f, 0.80f));
            Button tab2 = CreateButton("Tab 2", panel.transform, "FLOOR 2", font,
                new Vector2(0.36f, 0.71f), new Vector2(0.64f, 0.80f));
            Button tab3 = CreateButton("Tab 3", panel.transform, "FLOOR 3", font,
                new Vector2(0.67f, 0.71f), new Vector2(0.95f, 0.80f));
            Button layout = CreateButton("Layout", panel.transform, "LAYOUT: Living Area", font,
                new Vector2(0.05f, 0.61f), new Vector2(0.95f, 0.70f));
            Button rooms = CreateButton("Rooms", panel.transform, "ROOMS: 3", font,
                new Vector2(0.05f, 0.51f), new Vector2(0.95f, 0.60f));
            Button walls = CreateButton("Walls", panel.transform, "WALL COLOUR: Default", font,
                new Vector2(0.05f, 0.41f), new Vector2(0.95f, 0.50f));
            Image swatch = CreatePanel("Wall Swatch", walls.transform, Color.white,
                new Vector2(0.88f, 0.20f), new Vector2(0.97f, 0.80f));
            swatch.raycastTarget = false;
            Button flooring = CreateButton("Flooring", panel.transform, "FLOORING: Mixed", font,
                new Vector2(0.05f, 0.31f), new Vector2(0.95f, 0.40f));
            Button furniture = CreateButton("Furniture", panel.transform, "FURNITURE: On", font,
                new Vector2(0.05f, 0.21f), new Vector2(0.95f, 0.30f));
            Button build = CreateButton("Build", panel.transform, "BUILD", font,
                new Vector2(0.05f, 0.05f), new Vector2(0.48f, 0.17f));
            Button close = CreateButton("Close", panel.transform, "CLOSE", font,
                new Vector2(0.52f, 0.05f), new Vector2(0.95f, 0.17f));

            SerializedObject data = new SerializedObject(creator);
            data.FindProperty("panelRoot").objectReferenceValue = dim.gameObject;
            data.FindProperty("floorCountButton").objectReferenceValue = floorCount;
            data.FindProperty("tab1Button").objectReferenceValue = tab1;
            data.FindProperty("tab2Button").objectReferenceValue = tab2;
            data.FindProperty("tab3Button").objectReferenceValue = tab3;
            data.FindProperty("layoutButton").objectReferenceValue = layout;
            data.FindProperty("roomsButton").objectReferenceValue = rooms;
            data.FindProperty("wallsButton").objectReferenceValue = walls;
            data.FindProperty("wallSwatch").objectReferenceValue = swatch;
            data.FindProperty("flooringButton").objectReferenceValue = flooring;
            data.FindProperty("furnitureButton").objectReferenceValue = furniture;
            data.FindProperty("buildButton").objectReferenceValue = build;
            data.FindProperty("closeButton").objectReferenceValue = close;
            data.ApplyModifiedPropertiesWithoutUndo();

            dim.gameObject.SetActive(false);
        }

        private static Image CreatePanel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = panel.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string value, Font font, int size,
            FontStyle style, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = size;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Font font,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = buttonObject.GetComponent<Image>();
            image.color = Color.white;
            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.18f, 0.22f, 0.28f);
            colors.highlightedColor = new Color(0.20f, 0.55f, 0.72f);
            colors.pressedColor = new Color(0.08f, 0.42f, 0.58f);
            colors.disabledColor = new Color(0.12f, 0.14f, 0.16f, 0.75f);
            button.colors = colors;
            CreateText("Label", buttonObject.transform, label, font, 22, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Vector2(0.03f, 0f), new Vector2(0.97f, 1f));
            return button;
        }

        // ------------------------------------------------------------------ project settings

        private static void ConfigureAndroid()
        {
            PlayerSettings.companyName = "AR Blueprint";
            PlayerSettings.productName = "AR Building Blueprint Visualizer";
            PlayerSettings.bundleVersion = "1.2.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.arblueprint.visualizer");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        /// <summary>Sharper edges and tighter, softer shadows around the small building.</summary>
        private static void ConfigureURPQuality()
        {
            const string assetPath = "Assets/Settings/Mobile_RPAsset.asset";
            UniversalRenderPipelineAsset asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (asset == null)
                return;

            SerializedObject data = new SerializedObject(asset);
            SetInt(data, "m_MSAA", 4);
            SetInt(data, "m_MainLightShadowmapResolution", 2048);
            SetFloat(data, "m_ShadowDistance", 6f);
            SetBool(data, "m_SoftShadowsSupported", true);
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void SetInt(SerializedObject o, string name, int value)
        {
            SerializedProperty p = o.FindProperty(name);
            if (p != null) p.intValue = value;
        }

        private static void SetFloat(SerializedObject o, string name, float value)
        {
            SerializedProperty p = o.FindProperty(name);
            if (p != null) p.floatValue = value;
        }

        private static void SetBool(SerializedObject o, string name, bool value)
        {
            SerializedProperty p = o.FindProperty(name);
            if (p != null) p.boolValue = value;
        }

        private static void ConfigureURPCameraBackground()
        {
            const string rendererPath = "Assets/Settings/Mobile_Renderer.asset";
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
                throw new System.InvalidOperationException("The URP mobile renderer asset could not be found.");

            foreach (ScriptableRendererFeature existingFeature in rendererData.rendererFeatures)
            {
                if (existingFeature is ARBackgroundRendererFeature)
                    return;
            }

            ARBackgroundRendererFeature feature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
            feature.name = "AR Background Renderer Feature";
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            rendererData.rendererFeatures.Add(feature);
            EditorUtility.SetDirty(rendererData);
        }

        private static void ConfigureARCoreLoader()
        {
            const string settingsFolder = "Assets/XR";
            const string settingsPath = settingsFolder + "/XRGeneralSettingsPerBuildTarget.asset";
            Directory.CreateDirectory(settingsFolder);

            XRGeneralSettingsPerBuildTarget perBuildTarget =
                AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(settingsPath);
            if (perBuildTarget == null)
            {
                perBuildTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perBuildTarget, settingsPath);
            }

            if (!perBuildTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
                perBuildTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perBuildTarget, true);
            XRGeneralSettings settings = perBuildTarget.SettingsForBuildTarget(BuildTargetGroup.Android);

            const string loaderName = "UnityEngine.XR.ARCore.ARCoreLoader";
            if (!XRPackageMetadataStore.AssignLoader(settings.Manager, loaderName, BuildTargetGroup.Android))
                throw new System.InvalidOperationException("Could not assign the ARCore XR loader for Android.");

            EditorUtility.SetDirty(perBuildTarget);
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(settings.Manager);
        }
    }
}
#endif
