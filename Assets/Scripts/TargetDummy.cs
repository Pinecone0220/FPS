using System.Collections;
using TMPro;
using UnityEngine;

namespace FpsPractice.FPS
{
    [RequireComponent(typeof(Health))]
    public class TargetDummy : MonoBehaviour
    {
        [SerializeField] private Renderer[] highlightRenderers;
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private float flashDuration = 0.08f;
        [Header("Damage Popup")]
        [Tooltip("피해량 텍스트 시작 높이입니다.")]
        [SerializeField] private float damagePopupHeightOffset = 1.5f;
        [Tooltip("피해량 텍스트 크기입니다.")]
        [SerializeField] private float damagePopupFontSize = 3.5f;

        private Color[] originalColors;
        private Coroutine flashRoutine;
        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
            CacheOriginalColors();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
            }
        }

        public void FlashHit()
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                return;
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(FlashCoroutine());
        }

        private void HandleDamaged(float amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            FlashHit();
            SpawnDamagePopup(amount, hitPoint);
        }

        private void SpawnDamagePopup(float amount, Vector3 hitPoint)
        {
            Vector3 spawnPosition = transform.position;
            if (damagePopupHeightOffset != 0f)
            {
                spawnPosition += Vector3.up * damagePopupHeightOffset;
            }

            spawnPosition = Vector3.Lerp(spawnPosition, hitPoint, 0.25f);

            GameObject popupObject = new GameObject("Damage Popup");
            popupObject.transform.position = spawnPosition;
            popupObject.transform.rotation = Quaternion.identity;

            TextMeshPro textMesh = popupObject.AddComponent<TextMeshPro>();
            textMesh.text = Mathf.RoundToInt(amount).ToString();
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.fontSize = damagePopupFontSize;
            textMesh.enableAutoSizing = false;
            textMesh.color = Color.white;

            if (TMP_Settings.defaultFontAsset != null)
            {
                textMesh.font = TMP_Settings.defaultFontAsset;
            }

            DamagePopup popup = popupObject.AddComponent<DamagePopup>();
            popup.Initialize();
        }

        private void CacheOriginalColors()
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                return;
            }

            originalColors = new Color[highlightRenderers.Length];
            for (int i = 0; i < highlightRenderers.Length; i++)
            {
                if (highlightRenderers[i] != null && highlightRenderers[i].material != null)
                {
                    originalColors[i] = highlightRenderers[i].material.color;
                }
            }
        }

        private IEnumerator FlashCoroutine()
        {
            for (int i = 0; i < highlightRenderers.Length; i++)
            {
                if (highlightRenderers[i] != null && highlightRenderers[i].material != null)
                {
                    highlightRenderers[i].material.color = hitColor;
                }
            }

            yield return new WaitForSeconds(flashDuration);

            for (int i = 0; i < highlightRenderers.Length; i++)
            {
                if (highlightRenderers[i] != null && highlightRenderers[i].material != null)
                {
                    highlightRenderers[i].material.color = originalColors[i];
                }
            }

            flashRoutine = null;
        }
    }

    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private float lifetime = 0.9f;
        [SerializeField] private float riseSpeed = 1.25f;
        [SerializeField] private float sidewaysDrift = 0.12f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        private Camera targetCamera;
        private Color baseColor = Color.white;
        private float elapsedTime;
        private Vector3 moveDirection;

        private void Awake()
        {
            if (text == null)
            {
                text = GetComponent<TMP_Text>();
            }

            if (text == null)
            {
                text = GetComponentInChildren<TMP_Text>(true);
            }

            if (text != null)
            {
                baseColor = text.color;
            }
        }

        public void Initialize()
        {
            targetCamera = Camera.main;
            elapsedTime = 0f;
            moveDirection = (Vector3.up + new Vector3(Random.Range(-sidewaysDrift, sidewaysDrift), 0f, 0f)).normalized;
            FaceCamera();
        }

        private void LateUpdate()
        {
            if (text == null)
            {
                return;
            }

            elapsedTime += Time.deltaTime;
            transform.position += moveDirection * riseSpeed * Time.deltaTime;
            FaceCamera();

            float normalizedTime = lifetime <= 0f ? 1f : Mathf.Clamp01(elapsedTime / lifetime);
            float alpha = fadeCurve != null ? fadeCurve.Evaluate(normalizedTime) : Mathf.Clamp01(1f - normalizedTime);

            Color color = baseColor;
            color.a = alpha;
            text.color = color;

            if (normalizedTime >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void FaceCamera()
        {
            Camera cameraToFace = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToFace == null)
            {
                return;
            }

            Vector3 toCamera = cameraToFace.transform.position - transform.position;
            if (toCamera.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(toCamera, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
        }
    }
}
