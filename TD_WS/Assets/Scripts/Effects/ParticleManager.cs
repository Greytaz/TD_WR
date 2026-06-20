using UnityEngine;
using TowerDefense.Utils;

namespace TowerDefense.Effects
{
    public class ParticleManager : MonoBehaviour
    {
        public static ParticleManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SpawnParticle(string poolTag, Vector3 position, float duration = 2.0f)
        {
            GameObject particle = ObjectPool.Instance.SpawnFromPool(poolTag, position, Quaternion.identity);
            if (particle != null)
            {
                StartCoroutine(ReturnToPoolAfterDelay(particle, poolTag, duration));
            }
        }

        private System.Collections.IEnumerator ReturnToPoolAfterDelay(GameObject obj, string tag, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null && obj.activeSelf)
            {
                ObjectPool.Instance.ReturnToPool(obj, tag);
            }
        }
    }
}
