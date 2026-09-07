using UnityEngine;

namespace FpsPractice.FPS
{
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal);
    }
}
