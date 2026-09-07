using UnityEngine;

namespace FpsPractice.FPS
{
    [RequireComponent(typeof(BoxCollider))]
    public class WaveSpawnZone : MonoBehaviour
    {
        [SerializeField] private BoxCollider spawnBounds;

        private void Awake()
        {
            if (spawnBounds == null)
            {
                spawnBounds = GetComponent<BoxCollider>();
            }

            spawnBounds.isTrigger = true;
        }

        public Vector3 GetRandomPoint()
        {
            if (spawnBounds == null)
            {
                spawnBounds = GetComponent<BoxCollider>();
            }

            Vector3 localPoint = spawnBounds.center + new Vector3(
                Random.Range(-spawnBounds.size.x * 0.5f, spawnBounds.size.x * 0.5f),
                Random.Range(-spawnBounds.size.y * 0.5f, spawnBounds.size.y * 0.5f),
                Random.Range(-spawnBounds.size.z * 0.5f, spawnBounds.size.z * 0.5f));
            return transform.TransformPoint(localPoint);
        }

        private void OnDrawGizmosSelected()
        {
            if (spawnBounds == null)
            {
                spawnBounds = GetComponent<BoxCollider>();
            }

            if (spawnBounds == null)
            {
                return;
            }

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(spawnBounds.center, spawnBounds.size);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireCube(spawnBounds.center, spawnBounds.size);
            Gizmos.matrix = previousMatrix;
        }
    }
}