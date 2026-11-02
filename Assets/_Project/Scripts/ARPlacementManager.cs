using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using EnhancedTouchSupport = UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace ARBlueprint
{
    /// <summary>Places one building on a detected horizontal AR plane.</summary>
    public class ARPlacementManager : MonoBehaviour
    {
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private GameObject buildingPrefab;
        [SerializeField] private UIManager uiManager;

        private static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();
        private GameObject placedBuilding;

        public GameObject PlacedBuilding => placedBuilding;
        public bool HasPlacedBuilding => placedBuilding != null;

        private void Awake()
        {
            EnsureCameraPoseDriver();
            EnsureUIInputModule();
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            if (placedBuilding != null)
            {
                // Keep plane tracking active for movement, but hide its blue mesh.
                SetPlanesVisible(false);
                return;
            }

            UpdateScanStatus();

            var touches = Touch.activeTouches;
            if (touches.Count != 1)
                return;

            Touch touch = touches[0];
            if (touch.phase != TouchPhase.Began || IsTouchOverUI(touch.touchId))
                return;

            if (raycastManager.Raycast(touch.screenPosition, Hits, TrackableType.PlaneWithinPolygon))
                PlaceBuilding(Hits[0].pose);
            else
                uiManager?.SetStatus("No surface under your finger - tap inside the blue area");
        }

        private void PlaceBuilding(Pose pose)
        {
            placedBuilding = Instantiate(buildingPrefab, pose.position, pose.rotation);

            ARObjectManipulator manipulator = placedBuilding.GetComponent<ARObjectManipulator>();
            if (manipulator != null)
                manipulator.SetRaycastManager(raycastManager);

            BuildingController building = placedBuilding.GetComponent<BuildingController>();
            building.Initialize(BuildingConfig.Current);

            SetPlanesVisible(false);
            uiManager?.OnBuildingPlaced(building);
        }

        /// <summary>Builds the given floors; if a building is already placed it is rebuilt in place.</summary>
        public void ApplyBuildingSpec(BuildingSpec spec)
        {
            BuildingConfig.Current = spec.Clone();
            if (placedBuilding == null)
                return;

            placedBuilding.GetComponent<BuildingController>().Initialize(BuildingConfig.Current);
            uiManager?.OnBuildingRebuilt();
        }

        public void ResetPlacement()
        {
            if (placedBuilding != null)
                Destroy(placedBuilding);

            placedBuilding = null;
            SetPlanesVisible(true);
            uiManager?.OnPlacementReset();
        }

        private void SetPlanesVisible(bool visible)
        {
            foreach (ARPlane plane in planeManager.trackables)
            {
                MeshRenderer planeRenderer = plane.GetComponent<MeshRenderer>();
                if (planeRenderer != null)
                    planeRenderer.enabled = visible;
            }
        }

        /// <summary>Shows live AR state so a failed scan explains itself instead of staying silent.</summary>
        private void UpdateScanStatus()
        {
            if (uiManager == null)
                return;

            switch (ARSession.state)
            {
                case ARSessionState.Unsupported:
                    uiManager.SetStatus("This phone does not support ARCore");
                    return;
                case ARSessionState.NeedsInstall:
                case ARSessionState.Installing:
                    uiManager.SetStatus("Installing / updating Google Play Services for AR...");
                    return;
                case ARSessionState.CheckingAvailability:
                case ARSessionState.Ready:
                case ARSessionState.SessionInitializing:
                    uiManager.SetStatus("Starting AR session (allow camera access if asked)...");
                    return;
                case ARSessionState.None:
                    uiManager.SetStatus("Waiting for AR session...");
                    return;
            }

            if (planeManager.trackables.count > 0)
            {
                uiManager.SetStatus($"Surface detected ({planeManager.trackables.count}) - tap the blue area");
                return;
            }

            switch (ARSession.notTrackingReason)
            {
                case NotTrackingReason.InsufficientLight:
                    uiManager.SetStatus("Too dark - move to a brighter area");
                    break;
                case NotTrackingReason.ExcessiveMotion:
                    uiManager.SetStatus("Moving too fast - move the phone slowly");
                    break;
                case NotTrackingReason.InsufficientFeatures:
                    uiManager.SetStatus("Point at a textured, well-lit floor or table");
                    break;
                default:
                    uiManager.SetStatus("Scanning - sweep the phone slowly over a floor or table");
                    break;
            }
        }

        /// <summary>
        /// The AR camera must follow the physical device. The scene was generated without a pose driver,
        /// so the camera never moved and detected planes did not line up with the live camera image.
        /// </summary>
        private void EnsureCameraPoseDriver()
        {
            XROrigin origin = FindFirstObjectByType<XROrigin>();
            Camera arCamera = origin != null && origin.Camera != null ? origin.Camera : Camera.main;
            if (arCamera == null || arCamera.GetComponent<TrackedPoseDriver>() != null)
                return;

            var position = new InputAction("AR Position", InputActionType.Value, expectedControlType: "Vector3");
            position.AddBinding("<HandheldARInputDevice>/devicePosition");
            position.AddBinding("<XRHMD>/centerEyePosition");
            position.AddBinding("<XRHMD>/devicePosition");

            var rotation = new InputAction("AR Rotation", InputActionType.Value, expectedControlType: "Quaternion");
            rotation.AddBinding("<HandheldARInputDevice>/deviceRotation");
            rotation.AddBinding("<XRHMD>/centerEyeRotation");
            rotation.AddBinding("<XRHMD>/deviceRotation");

            var tracking = new InputAction("AR Tracking State", InputActionType.Value, expectedControlType: "Integer");
            tracking.AddBinding("<HandheldARInputDevice>/trackingState");
            tracking.AddBinding("<XRHMD>/trackingState");

            TrackedPoseDriver driver = arCamera.gameObject.AddComponent<TrackedPoseDriver>();
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            driver.ignoreTrackingState = true;
            driver.positionInput = new InputActionProperty(position);
            driver.rotationInput = new InputActionProperty(rotation);
            driver.trackingStateInput = new InputActionProperty(tracking);

            position.Enable();
            rotation.Enable();
            tracking.Enable();
        }

        /// <summary>The project uses the Input System package, which needs its own UI input module.</summary>
        private static void EnsureUIInputModule()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null || eventSystem.GetComponent<InputSystemUIInputModule>() != null)
                return;

            StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacy != null)
                DestroyImmediate(legacy);

            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        private static bool IsTouchOverUI(int touchId)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touchId);
        }
    }
}
