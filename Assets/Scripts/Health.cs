using System;
using System.Collections;
using UnityEngine;

namespace FpsPractice.FPS
{
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float armor;
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private bool respawnOnDeath = true;
        [SerializeField, Min(0f)] private float respawnDelay = 3f;
        [Tooltip("0보다 크면 사망 애니메이션이 재생되는 동안 삭제/비활성화를 기다립니다.")]
        [SerializeField, Min(0f)] private float deathAnimationDuration;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public float Armor => armor;
        public bool IsDead { get; private set; }
        public event Action<float, Vector3, Vector3> Damaged;
        public event Action Died;

        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Vector3 spawnScale;
        private float spawnArmor;
        private Renderer[] cachedRenderers;
        private Collider[] cachedColliders;
        private Rigidbody[] cachedRigidbodies;
        private Coroutine respawnRoutine;

        private void Awake()
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            spawnScale = transform.localScale;
            spawnArmor = armor;
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            cachedColliders = GetComponentsInChildren<Collider>(true);
            cachedRigidbodies = GetComponentsInChildren<Rigidbody>(true);
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            float damageToDistribute = armor > 0f ? amount * 0.66f : amount;
            float absorbedDamage = Mathf.Min(armor, damageToDistribute);
            armor -= absorbedDamage;
            float appliedDamage = Mathf.Max(1f, damageToDistribute - absorbedDamage);
            appliedDamage = Mathf.Min(appliedDamage, CurrentHealth);
            CurrentHealth = Mathf.Max(0f, CurrentHealth - appliedDamage);
            if (appliedDamage > 0f)
            {
                Damaged?.Invoke(appliedDamage, hitPoint, hitNormal);
            }

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }
        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }

        public void AddArmor(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            armor += amount;
        }

        public void ConfigureDeathBehavior(bool shouldDestroyOnDeath, bool shouldRespawnOnDeath, float newRespawnDelay)
        {
            destroyOnDeath = shouldDestroyOnDeath;
            respawnOnDeath = shouldRespawnOnDeath;
            respawnDelay = Mathf.Max(0f, newRespawnDelay);
        }

        public void SetDeathAnimationDuration(float duration)
        {
            deathAnimationDuration = Mathf.Max(0f, duration);
        }

        public void SetMaxHealth(float value, bool refillHealth = true)
        {
            maxHealth = Mathf.Max(1f, value);

            if (refillHealth)
            {
                CurrentHealth = maxHealth;
            }
        }

        public void ResetToFullHealth()
        {
            CurrentHealth = maxHealth;
            armor = spawnArmor;
            IsDead = false;
        }

        private void Die()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            Died?.Invoke();

            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }

            if (deathAnimationDuration > 0f)
            {
                respawnRoutine = StartCoroutine(DeathAnimationCoroutine());
                return;
            }

            FinishDeath();
        }

        private IEnumerator DeathAnimationCoroutine()
        {
            yield return new WaitForSeconds(deathAnimationDuration);
            FinishDeath();
        }

        private void FinishDeath()
        {
            SetDeathState(true);

            if (respawnOnDeath)
            {
                respawnRoutine = StartCoroutine(RespawnCoroutine());
                return;
            }

            if (destroyOnDeath)
            {
                Destroy(gameObject);
                return;
            }

            gameObject.SetActive(false);
        }

        private IEnumerator RespawnCoroutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            transform.localScale = spawnScale;
            CurrentHealth = maxHealth;
            armor = spawnArmor;
            IsDead = false;
            SetDeathState(false);
            respawnRoutine = null;
        }

        private void SetDeathState(bool isDead)
        {
            if (cachedRenderers != null)
            {
                for (int i = 0; i < cachedRenderers.Length; i++)
                {
                    if (cachedRenderers[i] != null)
                    {
                        cachedRenderers[i].enabled = !isDead;
                    }
                }
            }

            if (cachedColliders != null)
            {
                for (int i = 0; i < cachedColliders.Length; i++)
                {
                    if (cachedColliders[i] != null)
                    {
                        cachedColliders[i].enabled = !isDead;
                    }
                }
            }

            if (cachedRigidbodies != null)
            {
                for (int i = 0; i < cachedRigidbodies.Length; i++)
                {
                    if (cachedRigidbodies[i] != null)
                    {
                        cachedRigidbodies[i].isKinematic = isDead;
                        cachedRigidbodies[i].useGravity = !isDead;
                        if (isDead)
                        {
                            cachedRigidbodies[i].linearVelocity = Vector3.zero;
                            cachedRigidbodies[i].angularVelocity = Vector3.zero;
                        }
                    }
                }
            }
        }
    }
}
