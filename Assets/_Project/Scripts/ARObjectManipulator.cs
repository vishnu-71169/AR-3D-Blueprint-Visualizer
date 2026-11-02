using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace ARBlueprint
{
    /// <summary>Simple one-finger move plus two-finger rotate and scale controls.</summary>
    public class ARObjectManipulator : MonoBehaviour
    {
        [SerializeField] private float minimumScale = 0.5f;
        [SerializeField] private float maximumScale = 2.5f;
        [SerializeField] private float rotationSpeed = 1f;

        private static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();
        private ARRaycastManager raycastManager;
        private float startingFingerDistance;
        private float startingFingerAngle;
        private Vector3 startingScale;
        private Quaternion startingRotation;

        public void SetRaycastManager(ARRaycastManager manager)
        {
            raycastManager = manager;
        }

        private void Update()
        {
            if (raycastManager == null)
                return;

            var touches = Touch.activeTouches;
            if (touches.Count == 1)
                MoveWithOneFinger(touches[0]);
            else if (touches.Count >= 2)
                RotateAndScale(touches[0], touches[1]);
        }

        private void MoveWithOneFinger(Touch touch)
        {
            if (touch.phase != TouchPhase.Moved || IsTouchOverUI(touch.touchId))
                return;

            if (raycastManager.Raycast(touch.screenPosition, Hits, TrackableType.PlaneWithinPolygon))
                transform.position = Hits[0].pose.position;
        }

        private void RotateAndScale(Touch first, Touch second)
        {
            if (IsTouchOverUI(first.touchId) || IsTouchOverUI(second.touchId))
                return;

            Vector2 fingerVector = second.screenPosition - first.screenPosition;
            float distance = fingerVector.magnitude;
            float angle = Mathf.Atan2(fingerVector.y, fingerVector.x) * Mathf.Rad2Deg;

            if (first.phase == TouchPhase.Began || second.phase == TouchPhase.Began)
            {
                startingFingerDistance = Mathf.Max(distance, 1f);
                startingFingerAngle = angle;
                startingScale = transform.localScale;
                startingRotation = transform.rotation;
                return;
            }

            float scaleFactor = distance / startingFingerDistance;
            float targetScale = Mathf.Clamp(startingScale.x * scaleFactor, minimumScale, maximumScale);
            transform.localScale = Vector3.one * targetScale;

            float angleChange = (angle - startingFingerAngle) * rotationSpeed;
            transform.rotation = Quaternion.AngleAxis(-angleChange, Vector3.up) * startingRotation;
        }

        private static bool IsTouchOverUI(int touchId)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touchId);
        }
    }
}
