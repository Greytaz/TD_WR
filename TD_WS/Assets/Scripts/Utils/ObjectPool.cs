using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Utils
{
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        [System.Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int size;
        }

        public List<Pool> pools;
        private Dictionary<string, Queue<GameObject>> poolDictionary;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializePools();
        }

        private void InitializePools()
        {
            poolDictionary = new Dictionary<string, Queue<GameObject>>();

            foreach (var pool in pools)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();

                for (int i = 0; i < pool.size; i++)
                {
                    GameObject obj = Instantiate(pool.prefab, transform);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                poolDictionary.Add(pool.tag, objectPool);
            }
        }

        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
                return null;
            }

            GameObject objectToSpawn = null;

            // Search for an inactive object in the pool
            int count = poolDictionary[tag].Count;
            for (int i = 0; i < count; i++)
            {
                GameObject obj = poolDictionary[tag].Dequeue();
                poolDictionary[tag].Enqueue(obj);

                if (!obj.activeSelf)
                {
                    objectToSpawn = obj;
                    break;
                }
            }

            // If all objects are active, instantiate a new one to grow the pool dynamically
            if (objectToSpawn == null)
            {
                foreach (var pool in pools)
                {
                    if (pool.tag == tag)
                    {
                        objectToSpawn = Instantiate(pool.prefab, transform);
                        poolDictionary[tag].Enqueue(objectToSpawn);
                        break;
                    }
                }
            }

            if (objectToSpawn != null)
            {
                objectToSpawn.transform.position = position;
                objectToSpawn.transform.rotation = rotation;
                objectToSpawn.SetActive(true);
            }

            return objectToSpawn;
        }

        public void ReturnToPool(GameObject obj, string tag)
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);
        }
    }
}
