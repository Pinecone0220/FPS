using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace FpsPractice.FPS
{
    [RequireComponent(typeof(Health))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private string targetTag = "Player";

        [Header("Movement")]
        [Tooltip("플레이어를 아직 발견하지 않았을 때의 이동 속도입니다.")]
        [FormerlySerializedAs("wanderSpeed")]
        [SerializeField, Min(0f)] private float walkSpeed = 1.5f;
        [Tooltip("시야 또는 총소리로 경보된 뒤의 이동 속도입니다.")]
        [FormerlySerializedAs("moveSpeed")]
        [SerializeField, Min(0f)] private float runSpeed = 3f;
        [SerializeField, Min(0f)] private float rotateSpeed = 8f;
        [SerializeField, Min(0f)] private float stopDistance = 2f;
        [SerializeField] private bool useSimpleGravity = false;
        [SerializeField] private float gravity = -20f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string walkingParameter = "IsWalking";
        [SerializeField] private string runningParameter = "IsRunning";
        [Tooltip("공격 상태로 전환할 Animator Trigger 이름입니다.")]
        [SerializeField] private string attackTriggerParameter = "Attack";
        [Tooltip("사망 애니메이션이 끝날 때까지 좀비를 유지할 시간입니다. Death 클립 길이에 맞춥니다.")]
        [SerializeField, Min(0f)] private float deathAnimationDuration = 1.2f;

        [Header("Detection")]
        [SerializeField, Min(0f)] private float detectionRadius = 15f;
        [SerializeField, Range(1f, 180f)] private float detectionAngle = 90f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float attackRange = 2.2f;
        [SerializeField, Min(0f)] private float attackDamage = 10f;
        [SerializeField, Min(0f)] private float attackCooldown = 1.25f;
        [SerializeField, Min(0f)] private float attackWindup = 0.15f;

        [Header("Idle Audio")]
        [Tooltip("좀비의 3D idle 사운드를 재생할 AudioSource입니다.")]
        [SerializeField] private AudioSource idleAudioSource;
        [Tooltip("간격마다 무작위로 재생할 idle 사운드 3개입니다.")]
        [SerializeField] private AudioClip[] idleSounds = new AudioClip[3];
        [Tooltip("idle 사운드가 재생되는 간격입니다.")]
        [SerializeField, Min(0.1f)] private float idleSoundInterval = 5f;
        [Tooltip("이 거리 안에서만 idle 사운드가 들립니다.")]
        [SerializeField, Min(0.1f)] private float idleSoundMaxDistance = 15f;
        [Tooltip("이 거리 안에서는 idle 사운드가 최대 음량으로 들립니다.")]
        [SerializeField, Min(0.1f)] private float idleSoundMinDistance = 1f;
        [Tooltip("idle 사운드의 기본 음량입니다.")]
        [SerializeField, Range(0f, 1f)] private float idleSoundVolume = 1f;


        [Header("Line of Sight")]
        [SerializeField] private LayerMask visionMask = ~0;
        [SerializeField] private float eyeHeight = 1.4f;
        [SerializeField] private float targetEyeHeight = 1.2f;

        private Health selfHealth;
        private CharacterController characterController;
        private NavMeshAgent navMeshAgent;
        private float nextAttackTime;
        private float verticalVelocity;
        private bool isDead;
        private bool hasAlerted;
        private int attackTriggerHash;
        private int deathTriggerHash;
        private float nextIdleSoundTime;
        private bool idleAudioDisabledInLobby;

        private void Awake()
        {
            selfHealth = GetComponent<Health>();
            characterController = GetComponent<CharacterController>();
            navMeshAgent = GetComponent<NavMeshAgent>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            attackTriggerHash = Animator.StringToHash(attackTriggerParameter);
            deathTriggerHash = Animator.StringToHash("Death");
            if (selfHealth != null)
            {
                selfHealth.SetDeathAnimationDuration(deathAnimationDuration);
            }
            FindTarget();
            EnsureAgentOnNavMesh();
            ConfigureIdleAudio();
            idleAudioDisabledInLobby = SceneManager.GetActiveScene().name == "Lobby";
            if (idleAudioDisabledInLobby && idleAudioSource != null)
            {
                idleAudioSource.Stop();
            }
            nextIdleSoundTime = Time.time + Random.Range(0f, idleSoundInterval);

            if (selfHealth != null)
            {
                selfHealth.Died += HandleDied;
            }
        }

        private void OnDestroy()
        {
            if (selfHealth != null)
            {
                selfHealth.Died -= HandleDied;
            }

            CancelInvoke(nameof(PerformAttack));
        }

        private void Update()
        {
            if (selfHealth == null)
            {
                return;
            }

            if (isDead && !selfHealth.IsDead)
            {
                isDead = false;
                hasAlerted = false;
                nextAttackTime = 0f;
            }

            if (isDead || selfHealth.IsDead)
            {
                return;
            }

            UpdateIdleSound();

            if (target == null)
            {
                FindTarget();
            }

            if (target == null)
            {
                StopAgent();
                SetMovementAnimation(false, false);
                ApplyIdleMotion();
                return;
            }

            if (!hasAlerted && IsPlayerVisible())
            {
                hasAlerted = true;
            }

            float distance = Vector3.Distance(transform.position, target.position);
            if (distance <= attackRange && HasLineOfSight())
            {
                StopAgent();
                FaceTarget();
                SetMovementAnimation(false, false);
                TryAttack();
                return;
            }

            if (distance > stopDistance)
            {
                MoveTowardsPoint(target.position, hasAlerted ? runSpeed : walkSpeed);
                SetMovementAnimation(true, hasAlerted);
            }
            else
            {
                StopAgent();
                FaceTarget();
                SetMovementAnimation(false, false);
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void ConfigureForWave(Transform newTarget)
        {
            target = newTarget;
            hasAlerted = false;
            isDead = false;
            useSimpleGravity = true;
            EnsureAgentOnNavMesh();
        }

        public void HearGunshot(Vector3 noisePosition)
        {
            if (isDead || selfHealth == null || selfHealth.IsDead)
            {
                return;
            }

            hasAlerted = true;
        }
        private void FindTarget()
        {
            if (target != null)
            {
                return;
            }

            GameObject found = GameObject.FindGameObjectWithTag(targetTag);
            if (found != null)
            {
                target = found.transform;
            }
        }

        private bool IsPlayerVisible()
        {
            if (target == null)
            {
                return false;
            }

            float distance = Vector3.Distance(transform.position, target.position);
            return distance <= detectionRadius
                && IsTargetInsideCone()
                && HasLineOfSight();
        }

        private bool IsTargetInsideCone()
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            float angle = Vector3.Angle(transform.forward, toTarget.normalized);
            return angle <= detectionAngle * 0.5f;
        }

        private bool EnsureAgentOnNavMesh()
        {
            if (navMeshAgent == null || !navMeshAgent.enabled)
            {
                return false;
            }

            if (navMeshAgent.isOnNavMesh)
            {
                return true;
            }

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, navMeshAgent.areaMask))
            {
                navMeshAgent.Warp(hit.position);
                return navMeshAgent.isOnNavMesh;
            }

            return false;
        }

        private void MoveTowardsPoint(Vector3 destination, float speed)
        {
            if (navMeshAgent != null && navMeshAgent.enabled)
            {
                if (!EnsureAgentOnNavMesh())
                {
                    return;
                }

                navMeshAgent.isStopped = false;
                navMeshAgent.updateRotation = true;
                navMeshAgent.speed = speed;
                navMeshAgent.SetDestination(destination);
                return;
            }

            Vector3 direction = destination - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                ApplyIdleMotion();
                return;
            }

            Vector3 movement = direction.normalized * speed;
            ApplyGravity(ref movement);

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(movement * Time.deltaTime);
            }
            else
            {
                transform.position += movement * Time.deltaTime;
            }

            FacePosition(destination);
        }

        private void StopAgent()
        {
            if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
            {
                return;
            }

            navMeshAgent.isStopped = true;
            navMeshAgent.updateRotation = false;
            navMeshAgent.ResetPath();
        }

        private void ApplyGravity(ref Vector3 movement)
        {
            if (!useSimpleGravity)
            {
                return;
            }

            if (characterController != null && characterController.enabled
                && characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            movement.y = verticalVelocity;
        }

        private void ApplyIdleMotion()
        {
            if (navMeshAgent != null && navMeshAgent.enabled)
            {
                StopAgent();
                return;
            }

            if (!useSimpleGravity)
            {
                return;
            }

            Vector3 movement = Vector3.zero;
            ApplyGravity(ref movement);

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(movement * Time.deltaTime);
            }
            else
            {
                transform.position += movement * Time.deltaTime;
            }
        }

        private void FaceTarget()
        {
            if (target != null)
            {
                FacePosition(target.position);
            }
        }

        private void FacePosition(Vector3 position)
        {
            Vector3 lookDirection = position - transform.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotateSpeed);
        }

        private bool HasAnimatorParameter(int parameterHash, AnimatorControllerParameterType parameterType)
        {
            if (animator == null)
            {
                return false;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].nameHash == parameterHash && parameters[i].type == parameterType)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetMovementAnimation(bool moving, bool running)
        {
            if (animator == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(walkingParameter))
            {
                animator.SetBool(walkingParameter, moving && !running);
            }

            if (!string.IsNullOrEmpty(runningParameter))
            {
                animator.SetBool(runningParameter, moving && running);
            }
        }

        private bool HasLineOfSight()
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 destination = target.position + Vector3.up * targetEyeHeight;
            Vector3 direction = destination - origin;
            float distance = direction.magnitude;

            if (distance <= 0.001f)
            {
                return true;
            }

            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance,
                visionMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == target || hit.transform.IsChildOf(target))
                {
                    return true;
                }

                return hit.collider.GetComponentInParent<Health>() == target.GetComponentInParent<Health>();
            }

            return true;
        }

        private void ConfigureIdleAudio()
        {
            if (idleAudioSource == null)
            {
                idleAudioSource = GetComponent<AudioSource>();
            }

            if (idleAudioSource == null)
            {
                return;
            }

            idleAudioSource.playOnAwake = false;
            idleAudioSource.loop = false;
            idleAudioSource.spatialBlend = 1f;
            idleAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            idleAudioSource.minDistance = Mathf.Min(idleSoundMinDistance, idleSoundMaxDistance);
            idleAudioSource.maxDistance = Mathf.Max(idleSoundMinDistance, idleSoundMaxDistance);
            idleAudioSource.volume = idleSoundVolume;
        }

        private void UpdateIdleSound()
        {
            if (idleAudioDisabledInLobby || idleAudioSource == null || idleSounds == null || idleSounds.Length == 0 ||
                Time.time < nextIdleSoundTime)
            {
                return;
            }

            AudioClip selectedClip = idleSounds[Random.Range(0, idleSounds.Length)];
            if (selectedClip != null)
            {
                idleAudioSource.PlayOneShot(selectedClip);
            }

            nextIdleSoundTime = Time.time + Mathf.Max(0.1f, idleSoundInterval);
        }

        private void TryAttack()
        {
            if (Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            if (animator != null && HasAnimatorParameter(attackTriggerHash, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(attackTriggerHash);
            }

            Invoke(nameof(PerformAttack), attackWindup);
        }

        private void PerformAttack()
        {
            if (isDead || target == null)
            {
                return;
            }

            if (Vector3.Distance(transform.position, target.position) > attackRange)
            {
                return;
            }

            MonoBehaviour[] behaviours = target.GetComponentsInParent<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IDamageable damageable)
                {
                    Vector3 hitPoint = target.position;
                    Vector3 hitNormal = (target.position - transform.position).normalized;
                    damageable.TakeDamage(attackDamage, hitPoint, hitNormal);
                    return;
                }
            }
        }

        private void HandleDied()
        {
            isDead = true;
            CancelInvoke(nameof(PerformAttack));
            StopAgent();
            if (animator != null && HasAnimatorParameter(deathTriggerHash, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(deathTriggerHash);
            }
            SetMovementAnimation(false, false);

        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.15f, 0.9f);
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            Gizmos.DrawWireSphere(origin, detectionRadius);

            Vector3 leftEdge = Quaternion.Euler(0f, -detectionAngle * 0.5f, 0f) * forward;
            Vector3 rightEdge = Quaternion.Euler(0f, detectionAngle * 0.5f, 0f) * forward;
            Gizmos.DrawLine(origin, origin + leftEdge.normalized * detectionRadius);
            Gizmos.DrawLine(origin, origin + rightEdge.normalized * detectionRadius);

            float halfAngle = detectionAngle * 0.5f;
            float step = detectionAngle / 24f;
            Vector3 previous = origin
                + Quaternion.Euler(0f, -halfAngle, 0f) * forward * detectionRadius;

            for (int i = 1; i <= 24; i++)
            {
                float angle = -halfAngle + step * i;
                Vector3 current = origin
                    + Quaternion.Euler(0f, angle, 0f) * forward * detectionRadius;
                Gizmos.DrawLine(previous, current);
                previous = current;
            }
        }
    }
}