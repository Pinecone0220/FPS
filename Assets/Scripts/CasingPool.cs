using System.Collections.Generic;
using UnityEngine;

namespace FpsPractice.FPS
{
    public static class CasingPool
    {
        private const int Capacity = 30;
        private static readonly Dictionary<int, Pool> Pools = new Dictionary<int, Pool>();

        private sealed class Pool
        {
            public readonly List<GameObject> Instances = new List<GameObject>(Capacity);
            public int NextIndex;
            public Transform Root;
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation,
            Vector3 velocity, Vector3 angularVelocity, float lifetime, GameObject owner)
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

            EnsureCapacity(pool, prefab, owner);
            GameObject instance = pool.Instances[pool.NextIndex];
            pool.NextIndex = (pool.NextIndex + 1) % pool.Instances.Count;
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            Rigidbody body = instance.GetComponentInChildren<Rigidbody>(true);
            if (body != null)
            {
                body.linearVelocity = velocity;
                body.angularVelocity = angularVelocity;
            }

            CasingPoolSelfDestruct recycle = instance.GetComponent<CasingPoolSelfDestruct>();
            if (recycle == null)
            {
                recycle = instance.AddComponent<CasingPoolSelfDestruct>();
            }
            recycle.Begin(Mathf.Max(0.05f, lifetime));
            return instance;
        }

        private static void EnsureCapacity(Pool pool, GameObject prefab, GameObject owner)
        {
            if (pool.Instances.Count >= Capacity)
            {
                return;
            }

            if (pool.Root == null)
            {
                GameObject root = new GameObject($"{prefab.name}_CasingPool");
                Object.DontDestroyOnLoad(root);
                pool.Root = root.transform;
            }

            while (pool.Instances.Count < Capacity)
            {
                GameObject instance = Object.Instantiate(prefab, pool.Root, false);
                IgnoreOwnerCollisions(instance, owner);
                instance.SetActive(false);
                pool.Instances.Add(instance);
            }
        }

        private static void IgnoreOwnerCollisions(GameObject casing, GameObject owner)
        {
            Collider[] casingColliders = casing.GetComponentsInChildren<Collider>(true);
            List<Collider> ignoredColliders = new List<Collider>();

            if (owner != null)
            {
                ignoredColliders.AddRange(owner.GetComponentsInParent<Collider>(true));
                ignoredColliders.AddRange(owner.GetComponentsInChildren<Collider>(true));
            }

            AddTaggedColliders("Player", ignoredColliders);
            AddTaggedColliders("Enemy", ignoredColliders);
            AddTaggedColliders("Target", ignoredColliders);

            for (int i = 0; i < casingColliders.Length; i++)
            {
                for (int j = 0; j < ignoredColliders.Count; j++)
                {
                    if (casingColliders[i] != null && ignoredColliders[j] != null)
                    {
                        Physics.IgnoreCollision(casingColliders[i], ignoredColliders[j], true);
                    }
                }
            }
        }

        private static void AddTaggedColliders(string tag, List<Collider> colliders)
        {
            GameObject[] objects;
            try
            {
                objects = GameObject.FindGameObjectsWithTag(tag);
            }
            catch (UnityException)
            {
                return;
            }

            for (int i = 0; i < objects.Length; i++)
            {
                colliders.AddRange(objects[i].GetComponentsInChildren<Collider>(true));
            }
        }    }

    public sealed class CasingPoolSelfDestruct : MonoBehaviour
    {
        private bool armed;

        public void Begin(float lifetime)
        {
            armed = true;
            CancelInvoke(nameof(DisableCasing));
            Invoke(nameof(DisableCasing), lifetime);
        }

        private void DisableCasing()
        {
            armed = false;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (armed)
            {
                CancelInvoke(nameof(DisableCasing));
                armed = false;
            }
        }
    }
}