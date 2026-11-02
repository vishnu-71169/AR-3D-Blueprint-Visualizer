using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

namespace ARBlueprint
{
    /// <summary>
    /// Matches the virtual lighting to the real room using ARCore light estimation, so the textured
    /// materials pick up believable brightness, colour, and direction. Falls back to the static
    /// scene lighting when the device does not provide an estimate.
    /// </summary>
    public class ARLightEstimation : MonoBehaviour
    {
        [SerializeField] private ARCameraManager cameraManager;
        [SerializeField] private Light mainLight;
        [SerializeField] private float minimumIntensity = 0.5f;
        [SerializeField] private float maximumIntensity = 1.6f;

        private void OnEnable()
        {
            if (cameraManager == null)
                return;

            cameraManager.requestedLightEstimation = LightEstimation.AmbientIntensity
                                                     | LightEstimation.AmbientColor
                                                     | LightEstimation.AmbientSphericalHarmonics
                                                     | LightEstimation.MainLightDirection
                                                     | LightEstimation.MainLightIntensity;
            cameraManager.frameReceived += OnFrameReceived;
        }

        private void OnDisable()
        {
            if (cameraManager != null)
                cameraManager.frameReceived -= OnFrameReceived;
        }

        private void OnFrameReceived(ARCameraFrameEventArgs args)
        {
            ARLightEstimationData estimate = args.lightEstimation;

            if (estimate.ambientSphericalHarmonics.HasValue)
            {
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientProbe = estimate.ambientSphericalHarmonics.Value;
            }
            else if (estimate.averageBrightness.HasValue)
            {
                RenderSettings.ambientIntensity = Mathf.Clamp(estimate.averageBrightness.Value * 1.5f, 0.4f, 1.4f);
            }

            if (mainLight == null)
                return;

            if (estimate.mainLightDirection.HasValue && estimate.mainLightDirection.Value.sqrMagnitude > 0.001f)
                mainLight.transform.rotation = Quaternion.LookRotation(estimate.mainLightDirection.Value);

            if (estimate.mainLightColor.HasValue)
                mainLight.color = estimate.mainLightColor.Value;

            if (estimate.averageMainLightBrightness.HasValue)
            {
                mainLight.intensity = Mathf.Clamp(estimate.averageMainLightBrightness.Value * 2f,
                    minimumIntensity, maximumIntensity);
            }
        }
    }
}
