using UnityEngine;
using TowerDefense.Enemies;
using TowerDefense.Effects;

namespace TowerDefense.Projectiles
{
    public class HomingProjectile : ProjectileBase
    {
        [Header("Archer/Mage Setup")]
        public bool isElemental = false; // Mage projectile

        protected override void MoveTowardsTarget()
        {
            // Move directly towards the target position
            Vector3 targetDir = lastTargetPos - transform.position;
            float step = stats.projectileSpeed * Time.deltaTime;

            if (targetDir.magnitude <= step)
            {
                // Snap and hit
                transform.position = lastTargetPos;
                HitTarget();
            }
            else
            {
                transform.position += targetDir.normalized * step;
                // Rotate projectile towards motion
                if (targetDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(targetDir);
                }
            }
        }

        protected override void ApplyDamageAndEffects()
        {
            if (target != null && target.gameObject.activeInHierarchy)
            {
                DamageType dmgType = isElemental ? DamageType.Elemental : DamageType.Physical;
                target.TakeDamage(stats.damage, dmgType);

                // Apply Slow
                if (stats.slowFactor < 1f && stats.slowDuration > 0f)
                {
                    target.ApplyStatusEffect(StatusEffectType.Slow, stats.slowDuration, stats.slowFactor);
                }

                // Apply Stun (chance)
                if (stats.stunDuration > 0f && stats.stunChance > 0f)
                {
                    if (Random.value <= stats.stunChance)
                    {
                        target.ApplyStatusEffect(StatusEffectType.Stun, stats.stunDuration, 1.0f);
                    }
                }

                // Apply Burn
                if (stats.burnDamagePerSecond > 0f && stats.burnDuration > 0f)
                {
                    target.ApplyStatusEffect(StatusEffectType.Burn, stats.burnDuration, stats.burnDamagePerSecond);
                }
            }
        }
    }
}
