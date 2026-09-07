using System.Collections;
using UnityEngine;

namespace FpsPractice.FPS
{
    public class AimLabTargetSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AimLabModeManager modeManager;
        [SerializeField] private GameObject targetPrefab;
        [SerializeField] private Transform spawnOrigin;

        [Header("Spawn Space")]
        [Tooltip("생성 공간의 중심입니다. 비워두면 이 오브젝트 위치를 사용합니다.")]
        [SerializeField] private Vector3 localCenterOffset = Vector3.zero;
        [Tooltip("생성 공간의 가로/세로/깊이 크기입니다.")]
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(10f, 6f, 2f);
        [Tooltip("타겟이 뜨기까지의 대기시간입니다.")]
        [SerializeField] private float spawnDelay = 0.15f;
        [Tooltip("타겟이 화면에 남아있는 시간입니다.")]
        [SerializeField, Min(0f)] private float targetLifetime = 1.25f;
        [Tooltip("새로 뜨는 타겟의 체력입니다.")]
        [SerializeField] private float targetHealth = 1f;
        [Tooltip("타겟이 죽었을 때 오브젝트를 바로 삭제할지 여부입니다.")]
        [SerializeField] private bool destroyTargetOnDeath = true;

        [Header("Gizmos")]
        [SerializeField] private bool drawSpawnGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 1f, 0.35f);

        private GameObject currentTarget;
        private Health currentHealth;
        private Coroutine respawnRoutine;
        private Coroutine lifetimeRoutine;
        private bool isRunning;
        private bool isPaused;
        private bool currentTargetScored;
        private float currentTargetSpawnTime;

        private void Awake()
        {
            if (modeManager == null)
            {
                modeManager = FindFirstObjectByType<AimLabModeManager>(FindObjectsInactive.Include);
            }

            if (spawnOrigin == null)
            {
                spawnOrigin = transform;
            }
        }

        public void BeginPractice()
        {
            isRunning = true;
            SpawnNextTarget();
        }

        public void StopPractice()
        {
            isRunning = false;

            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }

            if (lifetimeRoutine != null)
            {
                StopCoroutine(lifetimeRoutine);
                lifetimeRoutine = null;
            }

            RegisterCurrentTargetAsMissIfNeeded();
            UnbindCurrentHealth();

            if (currentTarget != null)
            {
                Destroy(currentTarget);
                currentTarget = null;
            }
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        public void NotifyCurrentTargetDestroyed()
        {
            if (!isRunning)
            {
                return;
            }

            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
            }

            respawnRoutine = StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            float elapsed = 0f;
            while (elapsed < spawnDelay)
            {
                if (!isRunning)
                {
                    yield break;
                }

                if (isPaused)
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            SpawnNextTarget();
            respawnRoutine = null;
        }

        private void SpawnNextTarget()
        {
            if (!isRunning || targetPrefab == null)
            {
                return;
            }

            UnbindCurrentHealth();

            if (lifetimeRoutine != null)
            {
                StopCoroutine(lifetimeRoutine);
                lifetimeRoutine = null;
            }

            if (currentTarget != null)
            {
                Destroy(currentTarget);
            }

            Vector3 spawnPosition = GetSpawnPosition();
            Quaternion spawnRotation = GetSpawnRotation(spawnPosition);
            currentTarget = Instantiate(targetPrefab, spawnPosition, spawnRotation);
            currentTargetSpawnTime = Time.unscaledTime;
            currentTargetScored = false;

            if (modeManager != null)
            {
                modeManager.ReportTargetSpawned();
            }

            currentHealth = currentTarget.GetComponent<Health>();
            if (currentHealth == null)
            {
                currentHealth = currentTarget.AddComponent<Health>();
            }

            currentHealth.ConfigureDeathBehavior(destroyTargetOnDeath, false, 0f);
            currentHealth.SetMaxHealth(targetHealth, true);
            currentHealth.Damaged += HandleTargetDamaged;
            currentHealth.Died += HandleTargetDied;

            if (currentTarget.GetComponent<TargetDummy>() == null)
            {
                currentTarget.AddComponent<TargetDummy>();
            }

            lifetimeRoutine = StartCoroutine(TargetLifetimeRoutine());
        }

        private IEnumerator TargetLifetimeRoutine()
        {
            float elapsed = 0f;
            while (elapsed < targetLifetime)
            {
                if (!isRunning)
                {
                    yield break;
                }

                if (isPaused)
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!isRunning || currentTarget == null)
            {
                yield break;
            }

            ExpireCurrentTarget();
            lifetimeRoutine = null;
        }

        private void ExpireCurrentTarget()
        {
            if (lifetimeRoutine != null)
            {
                StopCoroutine(lifetimeRoutine);
                lifetimeRoutine = null;
            }

            RegisterCurrentTargetAsMissIfNeeded();
            UnbindCurrentHealth();

            if (currentTarget != null)
            {
                Destroy(currentTarget);
                currentTarget = null;
            }

            currentHealth = null;
            NotifyCurrentTargetDestroyed();
        }

        private void RegisterCurrentTargetAsMissIfNeeded()
        {
            if (currentTarget != null && !currentTargetScored && modeManager != null)
            {
                modeManager.ReportTargetMissed();
            }

            currentTargetScored = false;
        }

        private void HandleTargetDamaged(float amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!isRunning || currentTargetScored)
            {
                return;
            }

            currentTargetScored = true;

            if (modeManager != null)
            {
                float reactionTime = Mathf.Max(0f, Time.unscaledTime - currentTargetSpawnTime);
                modeManager.ReportHit(reactionTime);
            }
        }

        private void HandleTargetDied()
        {
            if (lifetimeRoutine != null)
            {
                StopCoroutine(lifetimeRoutine);
                lifetimeRoutine = null;
            }

            if (currentHealth != null)
            {
                currentHealth.Damaged -= HandleTargetDamaged;
                currentHealth.Died -= HandleTargetDied;
            }

            currentHealth = null;
            currentTarget = null;
            NotifyCurrentTargetDestroyed();
        }

        private void UnbindCurrentHealth()
        {
            if (currentHealth != null)
            {
                currentHealth.Damaged -= HandleTargetDamaged;
                currentHealth.Died -= HandleTargetDied;
                currentHealth = null;
            }
        }

        private Vector3 GetSpawnPosition()
        {
            Transform reference = spawnOrigin != null ? spawnOrigin : transform;
            Vector3 halfSize = spawnAreaSize * 0.5f;
            Vector3 localOffset = new Vector3(
                Random.Range(-halfSize.x, halfSize.x),
                Random.Range(-halfSize.y, halfSize.y),
                Random.Range(-halfSize.z, halfSize.z));

            return reference.TransformPoint(localCenterOffset + localOffset);
        }

        private Quaternion GetSpawnRotation(Vector3 spawnPosition)
        {
            Transform reference = spawnOrigin != null ? spawnOrigin : transform;
            Vector3 toCenter = (reference.TransformPoint(localCenterOffset) - spawnPosition).normalized;
            Vector3 forward = toCenter.sqrMagnitude > 0.0001f ? toCenter : reference.forward;
            return Quaternion.LookRotation(forward, reference.up);
        }

        private void OnDrawGizmos()
        {
            if (!drawSpawnGizmos)
            {
                return;
            }

            Transform reference = spawnOrigin != null ? spawnOrigin : transform;
            Vector3 center = localCenterOffset;
            Vector3 size = spawnAreaSize;

            Color oldColor = Gizmos.color;
            Matrix4x4 oldMatrix = Gizmos.matrix;

            Gizmos.color = gizmoColor;
            Gizmos.matrix = Matrix4x4.TRS(reference.position, reference.rotation, Vector3.one);
            Gizmos.DrawWireCube(center, size);

            Gizmos.matrix = oldMatrix;
            Gizmos.color = oldColor;
        }
    }
}
