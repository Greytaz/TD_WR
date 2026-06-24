using UnityEngine;
using TowerDefense.Enemies;
using TowerDefense.Effects;

namespace TowerDefense.Projectiles
{
    public class RocketProjectile : ProjectileBase
    {
        private void Start()
        {
            // Add a smoke/fire trail renderer to look like a real rocket launch!
            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
                trail.time = 0.3f;
                trail.startWidth = 0.15f;
                trail.endWidth = 0.01f;
                
                Shader trailShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (trailShader == null) trailShader = Shader.Find("Sprites/Default");
                trail.material = new Material(trailShader);
                
                trail.startColor = new Color(1f, 0.5f, 0f, 0.8f); // Bright Orange rocket fire
                trail.endColor = new Color(0.2f, 0.2f, 0.2f, 0f); // Fading smoke
            }
        }

        protected override void MoveTowardsTarget()
        {
            // Move directly towards the target position (straight flight, no arc)
            Vector3 targetDir = lastTargetPos - transform.position;
            float step = stats.projectileSpeed * Time.deltaTime;

            if (targetDir.magnitude <= step)
            {
                transform.position = lastTargetPos;
                HitTarget();
            }
            else
            {
                transform.position += targetDir.normalized * step;
                if (targetDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(targetDir);
                }
            }
        }

        protected override void ApplyDamageAndEffects()
        {
            // Splash AOE damage like original Cannon projectile
            Collider[] colliders = Physics.OverlapSphere(transform.position, stats.splashRadius);
            float dmg = stats.GetRandomDamage(out bool isCrit);

            foreach (var col in colliders)
            {
                EnemyHealth enemy = col.GetComponent<EnemyHealth>();
                if (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    enemy.TakeDamage(dmg, DamageType.Explosive, isCrit);

                    if (stats.burnDamagePerSecond > 0f && stats.burnDuration > 0f)
                    {
                        enemy.ApplyStatusEffect(StatusEffectType.Burn, stats.burnDuration, stats.burnDamagePerSecond);
                    }
                }
            }
        }
    }
}