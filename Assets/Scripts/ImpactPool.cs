using System.Collections.Generic;
using UnityEngine;

namespace FpsPractice.FPS
{
    public static class ImpactPool
    {
        private const int DefaultCapacity = 30;
        private static readonly Dictionary<int, Pool> Pools = new Dictionary<int, Pool>();

        private sealed class Pool
        {
            public readonly List<GameObject> Instances = new List<GameObject>(DefaultCapacity);
            public int NextIndex;
            public Transform Root;
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
        {
            if (prefab == null)
            {
                return null;
            }

            int key = prefab.GetInstanceID();
            if (!Pools.TryGetValue(key, out Pool pool) || pool == null)
            {
                pool = new Pool();
                Pools[key] = pool;
            }

            EnsureCapacity(pool, prefab);

            if (pool.Instances.Count == 0)
            {
                return null;
            }

            GameObject instance = pool.Instances[pool.NextIndex];
            pool.NextIndex = (pool.NextIndex + 1) % pool.Instances.Count;

            instance.transform.SetPositionAndRotation(position, rotation);
            if (!instance.activeSelf)
            {
                instance.SetActive(true);
            }

            var particleSystem = instance.GetComponentInChildren<ParticleSystem>(true);
            if (particleSystem != null)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Play(true);
            }

            if (lifetime > 0f)
            {
                var selfDestruct = instance.GetComponent<ImpactPoolSelfDestruct>();
                if (selfDestruct == null)
                {
                    selfDestruct = instance.AddComponent<ImpactPoolSelfDestruct>();
                }

                selfDestruct.Begin(lifetime);
            }

            return instance;
        }

        private static void EnsureCapacity(Pool pool, GameObject prefab)
        {
            if (pool.Instances.Count >= DefaultCapacity)
            {
                return;
            }

            if (pool.Root == null)
            {
                GameObject rootObject = new GameObject($"{prefab.name}_ImpactPool");
                Object.DontDestroyOnLoad(rootObject);
                pool.Root = rootObject.transform;
            }

            while (pool.Instances.Count < DefaultCapacity)
            {
                GameObject instance = Object.Instantiate(prefab, pool.Root, false);
                instance.SetActive(false);
                pool.Instances.Add(instance);
            }
        }
    }

    public sealed class ImpactPoolSelfDestruct : MonoBehaviour
    {
        private float disableAt;
        private bool isArmed;

        public void Begin(float lifetime)
        {
            disableAt = Time.time + lifetime;
            isArmed = true;
            CancelInvoke(nameof(DisableSelf));
            Invoke(nameof(DisableSelf), lifetime);
        }

        private void DisableSelf()
        {
            isArmed = false;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (isArmed)
            {
                CancelInvoke(nameof(DisableSelf));
                isArmed = false;
            }
        }
    }
}
