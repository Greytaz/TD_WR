using UnityEngine;
using TowerDefense.Enemies;
using TowerDefense.Data;
using TowerDefense.Effects;
using TowerDefense.Utils;

namespace TowerDefense.Projectiles
{
    public abstract class ProjectileBase : MonoBehaviour
    {
        public string poolTag; // Tag for recycling

        protected EnemyHealth target;
        protected TowerTierData stats;
        protected Vector3 lastTargetPos;
        protected bool isInitialized = false;

        public virtual void Initialize(EnemyHealth targetEnemy, TowerTierData towerStats)
        {
            target = targetEnemy;
            stats = towerStats;
            if (target != null)
            {
                lastTargetPos = target.transform.position;
            }
            isInitialized = true;
        }

        protected virtual void Update()
        {
            if (!isInitialized) return;

            if (target != null && target.gameObject.activeInHierarchy)
            {
                lastTargetPos = target.transform.position;
            }

            MoveTowardsTarget();
        }

        protected abstract void MoveTowardsTarget();

        protected virtual void HitTarget()
        {
            isInitialized = false;

            // Spawn Impact VFX
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.SpawnParticle("ImpactBurst", transform.position, 1.0f);
            }

            // Apply Damage and Status Effects
            ApplyDamageAndEffects();

            // Return to pool
            ObjectPool.Instance.ReturnToPool(gameObject, poolTag);
        }

        protected virtual void ApplyDamageAndEffects()
        {
            if (target != null && target.gameObject.activeInHierarchy)
            {
                // General default single target hit (e.g. Archer physical arrow)
                float dmg = stats.GetRandomDamage(out bool isCrit);
                target.TakeDamage(dmg, DamageType.Physical, isCrit);
            }
        }
    }
}
