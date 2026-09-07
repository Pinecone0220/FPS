using System.Collections.Generic;
using UnityEngine;

namespace FpsPractice.FPS
{
    public static class GunshotNoise
    {
        public static void Emit(Vector3 position, float radius)
        {
            if (radius <= 0f)
            {
                return;
            }

            Collider[] colliders = Physics.OverlapSphere(
                position,
                radius,
                ~0,
                QueryTriggerInteraction.Ignore);

            HashSet<EnemyAI> enemies = new HashSet<EnemyAI>();
            for (int i = 0; i < colliders.Length; i++)
            {
                EnemyAI enemy = colliders[i].GetComponentInParent<EnemyAI>();
                if (enemy != null)
                {
                    enemies.Add(enemy);
                }
            }

            foreach (EnemyAI enemy in enemies)
            {
                enemy.HearGunshot(position);
            }
        }
    }
}