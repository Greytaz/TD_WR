using UnityEngine;
using TowerDefense.Enemies;
using TowerDefense.Effects;

namespace TowerDefense.Projectiles
{
    public class SplashProjectile : ProjectileBase
    {
        [Header("Cannon Setup")]
        public float arcHeight = 2.0f; // Height of parabolic arc

        private Vector3 startPos;
        private float distanceToTarget;

        public override void Initialize(EnemyHealth targetEnemy, Data.TowerTierData towerStats)
        {
            base.Initialize(targetEnemy, towerStats);
            startPos = transform.position;
            distanceToTarget = Vector3.Distance(startPos, lastTargetPos);
        }

        protected override void MoveTowardsTarget()
        {
            // Parabolic arc movement
            Vector3 currentPos = transform.position;
            Vector3 targetPos = lastTargetPos;

            // Simple linear move in X-Z plane, and height arc in Y
            Vector3 linearPos = Vector3.MoveTowards(
                new Vector3(currentPos.x, startPos.y, currentPos.z), 
                new Vector3(targetPos.x, startPos.y, targetPos.z), 
                stats.projectileSpeed * Time.deltaTime
            );

            // Compute percentage of distance traversed
            float totalLinearDist = Vector3.Distance(new Vector3(startPos.x, startPos.y, startPos.z), new Vector3(targetPos.x, startPos.y, targetPos.z));
            float distTraversed = Vector3.Distance(new Vector3(startPos.x, startPos.y, startPos.z), linearPos);
            float pct = totalLinearDist > 0.01f ? (distTraversed / totalLinearDist) : 1.0f;

            // Height formula: arcHeight * sin(pi * pct)
            float height = Mathf.Sin(pct * Mathf.PI) * arcHeight;

            Vector3 nextPos = new Vector3(linearPos.x, startPos.y + height, linearPos.z);
            Vector3 dir = nextPos - transform.position;

            transform.position = nextPos;

            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }

            if (pct >= 0.99f || Vector3.Distance(new Vector3(transform.position.x, startPos.y, transform.position.z), new Vector3(targetPos.x, startPos.y, targetPos.z)) < 0.1f)
            {
                HitTarget();
            }
        }

        protected override void ApplyDamageAndEffects()
        {
            // Radial splash damage using OverlapSphere
            Collider[] colliders = Physics.OverlapSphere(transform.position, stats.splashRadius);
            
            foreach (var col in colliders)
            {
                EnemyHealth enemy = col.GetComponent<EnemyHealth>();
                if (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    enemy.TakeDamage(stats.damage, DamageType.Explosive);

                    // Cannon upgrade slow or burn (DOT)
                    if (stats.burnDamagePerSecond > 0f && stats.burnDuration > 0f)
                    {
                        enemy.ApplyStatusEffect(StatusEffectType.Burn, stats.burnDuration, stats.burnDamagePerSecond);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (stats != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, stats.splashRadius);
            }
        }
    }
}
