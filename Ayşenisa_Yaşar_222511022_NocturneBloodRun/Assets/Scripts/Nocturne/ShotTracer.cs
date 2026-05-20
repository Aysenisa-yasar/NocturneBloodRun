using UnityEngine;

namespace Nocturne
{
    [RequireComponent(typeof(LineRenderer))]
    public class ShotTracer : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 0.12f;

        private LineRenderer lineRenderer;
        private float elapsed;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / Mathf.Max(lifeTime, 0.001f));

            if (lineRenderer != null)
            {
                Color fadedColor = lineRenderer.startColor;
                fadedColor.a = 1f - normalized;
                lineRenderer.startColor = fadedColor;
                lineRenderer.endColor = fadedColor;
            }

            if (elapsed >= lifeTime)
            {
                Destroy(gameObject);
            }
        }

        public void Configure(Vector3 startPoint, Vector3 endPoint, Color color)
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            elapsed = 0f;
        }
    }
}
