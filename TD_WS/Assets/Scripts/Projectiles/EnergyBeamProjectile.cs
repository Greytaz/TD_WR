using UnityEngine;
using TowerDefense.Enemies;
using TowerDefense.Effects;
using TowerDefense.Data;

namespace TowerDefense.Projectiles
{
    public class EnergyBeamProjectile : ProjectileBase
    {
        private LineRenderer lineRenderer;
        private float lifeTime = 0.15f;
        private float timer = 0f;
        private Color beamColor = new Color(0.6f, 0.2f, 1f, 1f); // Beautiful Purple/Energy neon

        public override void Initialize(EnemyHealth targetEnemy, TowerTierData towerStats)
        {
            // Set base target/stats
            base.Initialize(targetEnemy, towerStats);

            // Apply Damage and Status Effects instantly
            ApplyDamageAndEffects();

            // Set up LineRenderer
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            // Configure LineRenderer
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.15f;
            lineRenderer.endWidth = 0.05f;

            // Simple bright unlit material
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            lineRenderer.material = new Material(unlitShader);
            
            lineRenderer.startColor = beamColor;
            lineRenderer.endColor = beamColor;

            // Spawn impact particles at target
            if (Effects.ParticleManager.Instance != null && target != null)
            {
                Effects.ParticleManager.Instance.SpawnParticle("ImpactBurst", target.transform.position, 1.0f);
            }

            timer = lifeTime;
            UpdateBeamPositions();
        }

        protected override void Update()
        {
            if (!isInitialized) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                isInitialized = false;
                if (lineRenderer != null) lineRenderer.enabled = false;
                // Return to pool
                Utils.ObjectPool.Instance.ReturnToPool(gameObject, poolTag);
                return;
            }

            // Fade beam
            if (lineRenderer != null)
            {
                float alpha = timer / lifeTime;
                Color faded = beamColor;
                faded.a = alpha;
                lineRenderer.startColor = faded;
                lineRenderer.endColor = faded;
                lineRenderer.startWidth = 0.15f * alpha;
                lineRenderer.endWidth = 0.05f * alpha;
            }

            UpdateBeamPositions();
        }

        private void UpdateBeamPositions()
        {
            if (lineRenderer == null) return;
            lineRenderer.enabled = true;

            // Line index 0 is our firePoint (transform.position)
            lineRenderer.SetPosition(0, transform.position);

            // Line index 1 is target position (or last known position)
            if (target != null && target.gameObject.activeInHierarchy)
            {
                lineRenderer.SetPosition(1, target.transform.position + Vector3.up * 0.5f); // Hit center of enemy
            }
            else
            {
                lineRenderer.SetPosition(1, lastTargetPos + Vector3.up * 0.5f);
            }
        }

        protected override void MoveTowardsTarget()
        {
            // Instant hit, no moving needed
        }

        protected override void ApplyDamageAndEffects()
        {
            if (target != null && target.gameObject.activeInHierarchy)
            {
                float dmg = stats.GetRandomDamage(out bool isCrit);
                target.TakeDamage(dmg, DamageType.Elemental, isCrit);

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