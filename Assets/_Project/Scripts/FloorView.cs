using System.Collections;
using UnityEngine;

namespace ARBlueprint
{
    /// <summary>One generated floor: a flat 2D blueprint and the 3D model that is extruded from it.</summary>
    public class FloorView : MonoBehaviour
    {
        private const float ExtrudeSeconds = 0.9f;
        private const float FlatScale = 0.01f;

        private Coroutine running;

        public Transform Model3D { get; private set; }
        public Transform Plan2D { get; private set; }
        public string DisplayName { get; private set; }
        public bool Is3D { get; private set; }

        public void Setup(Transform model, Transform plan, string displayName)
        {
            Model3D = model;
            Plan2D = plan;
            DisplayName = displayName;
            ShowMode(false, false);
        }

        /// <summary>Shows the blueprint (2D) or the model (3D); the walls rise from the plan when animated.</summary>
        public void ShowMode(bool show3D, bool animate)
        {
            if (running != null)
            {
                StopCoroutine(running);
                running = null;
            }

            Is3D = show3D;

            if (!animate || !isActiveAndEnabled)
            {
                Model3D.localScale = Vector3.one;
                Model3D.gameObject.SetActive(show3D);
                Plan2D.gameObject.SetActive(!show3D);
                return;
            }

            running = StartCoroutine(show3D ? Extrude() : Flatten());
        }

        private IEnumerator Extrude()
        {
            Plan2D.gameObject.SetActive(true);
            Model3D.gameObject.SetActive(true);
            yield return Scale(FlatScale, 1f);
            Plan2D.gameObject.SetActive(false);
            running = null;
        }

        private IEnumerator Flatten()
        {
            Plan2D.gameObject.SetActive(true);
            Model3D.gameObject.SetActive(true);
            yield return Scale(1f, FlatScale);
            Model3D.gameObject.SetActive(false);
            Model3D.localScale = Vector3.one;
            running = null;
        }

        private IEnumerator Scale(float from, float to)
        {
            float elapsed = 0f;
            while (elapsed < ExtrudeSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / ExtrudeSeconds));
                Model3D.localScale = new Vector3(1f, Mathf.Lerp(from, to, t), 1f);
                yield return null;
            }

            Model3D.localScale = new Vector3(1f, to, 1f);
        }
    }
}
