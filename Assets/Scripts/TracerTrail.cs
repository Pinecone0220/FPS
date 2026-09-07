using UnityEngine;

namespace FpsPractice.FPS
{
    [RequireComponent(typeof(TrailRenderer))]
    public class TracerTrail : MonoBehaviour
    {
        [Header("Render")]
        [SerializeField] private Material trailMaterial;
        [SerializeField] private Color trailColor = new Color(1f, 0.96f, 0.72f, 1f);
        [SerializeField] private float headWidth = 0.025f;
        [SerializeField] private float tailWidth = 0.0075f;
        [SerializeField] private float trailLifetime = 0.12f;
        [SerializeField] private int sortingOrder = 100;
        [SerializeField] private bool generateLightingData = false;

        [Header("Motion")]
        [SerializeField] private float destroyDelay = 0.2f;

        private TrailRenderer trailRenderer;
        private Vector3 startPosition;
        private Vector3 endPosition;
        private float travelDuration;
        private float travelTimer;
        private bool isInitialized;

        private void Awake()
        {
            trailRenderer = GetComponent<TrailRenderer>();
            ConfigureTrail();
        }

        private void OnValidate()
        {
            if (trailRenderer == null)
            {
                trailRenderer = GetComponent<TrailRenderer>();
            }

            ConfigureTrail();
        }

        private void Update()
        {
            if (!isInitialized || trailRenderer == null)
            {
                return;
            }

            travelTimer += Time.deltaTime;
            float t = travelDuration > 0f ? Mathf.Clamp01(travelTimer / travelDuration) : 1f;

            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            transform.rotation = Quaternion.LookRotation((endPosition - startPosition).normalized, Vector3.up);

            if (travelTimer >= travelDuration)
            {
                isInitialized = false;
                Destroy(gameObject, destroyDelay);
            }
        }

        public void Initialize(Vector3 start, Vector3 end, float lifetime, float speed)
        {
            startPosition = start;
            endPosition = end;
            trailLifetime = Mathf.Max(0.01f, lifetime);
            travelDuration = Mathf.Max(0.01f, Vector3.Distance(startPosition, endPosition) / Mathf.Max(0.01f, speed));
            travelTimer = 0f;
            isInitialized = true;

            if (trailRenderer == null)
            {
                trailRenderer = GetComponent<TrailRenderer>();
            }

            transform.position = startPosition;
            trailRenderer.Clear();
            trailRenderer.emitting = true;
            trailRenderer.time = Mathf.Max(trailLifetime, travelDuration);
            ConfigureTrail();
        }

        private void ConfigureTrail()
        {
            if (trailRenderer == null)
            {
                return;
            }

            trailRenderer.enabled = true;
            trailRenderer.alignment = LineAlignment.View;
            trailRenderer.textureMode = LineTextureMode.Stretch;
            trailRenderer.time = Mathf.Max(trailLifetime, travelDuration);
            trailRenderer.minVertexDistance = 0.01f;
            trailRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, Mathf.Clamp01(tailWidth / Mathf.Max(0.0001f, headWidth))));
            trailRenderer.widthMultiplier = headWidth;
            trailRenderer.numCornerVertices = 2;
            trailRenderer.numCapVertices = 6;
            trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trailRenderer.receiveShadows = false;
            trailRenderer.sortingOrder = sortingOrder;
            trailRenderer.generateLightingData = generateLightingData;

            if (trailMaterial != null)
            {
                trailRenderer.sharedMaterial = trailMaterial;
            }

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(trailColor, 0f),
                    new GradientColorKey(trailColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.15f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                });
            trailRenderer.colorGradient = gradient;
        }
    }
}
