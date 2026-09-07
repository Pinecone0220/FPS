using UnityEngine;

namespace FpsPractice.FPS
{
    public class DamageHitbox : MonoBehaviour
    {
        [Tooltip("이 부위에 적용할 피해 배율입니다.")]
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;

        private Health health;

        private void Awake()
        {
            health = GetComponentInParent<Health>();
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            if (health != null)
            {
                health.TakeDamage(amount * damageMultiplier, hitPoint, hitNormal);
            }
        }
    }
}