using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Data;
using TowerDefense.Effects;
using TowerDefense.Utils;

namespace TowerDefense.Enemies
{
    public class EnemyHealth : MonoBehaviour
    {
        [Header("References")]
        public EnemyData enemyData;
        public Transform healthBarForeground; // Scale on X axis for percentage

        private float currentHealth;
        private float maxHealth;
        private bool isDead = false;

        private List<ActiveStatusEffect> activeEffects = new List<ActiveStatusEffect>();
        private EnemyMovement enemyMovement;
        private Renderer enemyRenderer;
        private Color originalColor;

        private void Awake()
        {
            enemyMovement = GetComponent<EnemyMovement>();
            enemyRenderer = GetComponentInChildren<Renderer>();
            if (enemyRenderer != null)
            {
                originalColor = enemyRenderer.material.color;
            }
        }

        public void Initialize(float waveHealthMultiplier)
        {
            isDead = false;
            maxHealth = enemyData.maxHealth * waveHealthMultiplier;
            currentHealth = maxHealth;
            activeEffects.Clear();
            
            if (enemyRenderer != null)
            {
                enemyRenderer.material.color = originalColor;
            }

            UpdateHealthBar();
        }

        public float GetCurrentHealth() => currentHealth;

        private void Update()
        {
            if (isDead) return;

            // Update status effects
            HandleStatusEffects();
        }

        public void TakeDamage(float damage, DamageType type)
        {
            if (isDead) return;

            // Apply resistance
            float multiplier = 1f;
            switch (type)
            {
                case DamageType.Physical:
                    multiplier = enemyData.physicalResistance;
                    break;
                case DamageType.Explosive:
                    multiplier = enemyData.explosiveResistance;
                    break;
                case DamageType.Elemental:
                    multiplier = enemyData.elementalResistance;
                    break;
            }

            float finalDamage = damage * multiplier;
            currentHealth -= finalDamage;
            UpdateHealthBar();

            // Spawn floating damage text
            if (ObjectPool.Instance != null)
            {
                Vector3 spawnPos = transform.position + new Vector3(Random.Range(-0.2f, 0.2f), 1.0f, Random.Range(-0.2f, 0.2f));
                GameObject floatObj = ObjectPool.Instance.SpawnFromPool("DamageText", spawnPos, Quaternion.identity);
                if (floatObj != null)
                {
                    FloatingText ft = floatObj.GetComponent<FloatingText>();
                    if (ft != null)
                    {
                        Color color = Color.white;
                        switch (type)
                        {
                            case DamageType.Physical:
                                color = new Color(1f, 0.8f, 0.2f); // Orange/Yellow for Physical
                                break;
                            case DamageType.Explosive:
                                color = new Color(1f, 0.3f, 0.1f); // Red/Orange for Explosive
                                break;
                            case DamageType.Elemental:
                                color = new Color(0.2f, 0.6f, 1f); // Blue for Elemental
                                break;
                        }
                        ft.Setup(Mathf.RoundToInt(finalDamage).ToString(), color);
                    }
                }
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void ApplyStatusEffect(StatusEffectType type, float duration, float value)
        {
            if (isDead) return;

            // Check if we already have this effect active
            ActiveStatusEffect existing = activeEffects.Find(e => e.type == type);
            if (existing != null)
            {
                // Refresh duration and take stronger value if applicable
                existing.duration = Mathf.Max(existing.duration, duration);
                existing.timer = existing.duration;
                existing.value = Mathf.Max(existing.value, value);
            }
            else
            {
                ActiveStatusEffect newEffect = new ActiveStatusEffect(type, duration, value);
                activeEffects.Add(newEffect);
                
                if (type == StatusEffectType.Stun && enemyMovement != null)
                {
                    enemyMovement.SetStunned(true);
                }
            }

            UpdateVisualFeedback();
        }

        private void HandleStatusEffects()
        {
            bool hasSlow = false;
            float slowMod = 1f;

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                ActiveStatusEffect effect = activeEffects[i];
                effect.timer -= Time.deltaTime;

                if (effect.type == StatusEffectType.Burn)
                {
                    effect.tickTimer += Time.deltaTime;
                    if (effect.tickTimer >= 0.5f)
                    {
                        TakeDamage(effect.value * 0.5f, DamageType.Elemental); // tick damage
                        effect.tickTimer = 0f;
                    }
                }
                else if (effect.type == StatusEffectType.Slow)
                {
                    hasSlow = true;
                    slowMod = Mathf.Min(slowMod, effect.value);
                }

                if (effect.timer <= 0f)
                {
                    // Effect expired
                    if (effect.type == StatusEffectType.Stun && enemyMovement != null)
                    {
                        enemyMovement.SetStunned(false);
                    }
                    activeEffects.RemoveAt(i);
                    UpdateVisualFeedback();
                }
            }

            if (enemyMovement != null)
            {
                enemyMovement.SetSlowModifier(hasSlow ? slowMod : 1f);
            }
        }

        private void UpdateVisualFeedback()
        {
            if (enemyRenderer == null) return;

            bool isStunned = activeEffects.Exists(e => e.type == StatusEffectType.Stun);
            bool isBurning = activeEffects.Exists(e => e.type == StatusEffectType.Burn);
            bool isSlowed = activeEffects.Exists(e => e.type == StatusEffectType.Slow);

            if (isStunned)
            {
                enemyRenderer.material.color = Color.yellow;
            }
            else if (isBurning)
            {
                enemyRenderer.material.color = new Color(1f, 0.3f, 0f); // Orange
            }
            else if (isSlowed)
            {
                enemyRenderer.material.color = Color.cyan;
            }
            else
            {
                enemyRenderer.material.color = originalColor;
            }
        }

        private void UpdateHealthBar()
        {
            if (healthBarForeground != null)
            {
                float fill = Mathf.Clamp01(currentHealth / maxHealth);
                healthBarForeground.localScale = new Vector3(fill, 1f, 1f);
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            // Trigger reward
            EventBus.TriggerEnemyKilled(enemyData.goldReward);

            // Play death VFX
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.SpawnParticle("DeathBurst", transform.position, 1.5f);
            }

            StartCoroutine(FadeAndRecycle());
        }

        private System.Collections.IEnumerator FadeAndRecycle()
        {
            float elapsed = 0f;
            float duration = 2.0f; // corpse fade (2s)
            Vector3 startScale = transform.localScale;

            // Disable collider and movement during death fade
            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            
            if (enemyMovement != null)
            {
                enemyMovement.enabled = false;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float pct = elapsed / duration;
                
                // Scale down and fade color to simulate dissolving corpse
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, pct);
                
                if (enemyRenderer != null)
                {
                    Color c = enemyRenderer.material.color;
                    c.a = 1f - pct;
                    enemyRenderer.material.color = c;
                }

                yield return null;
            }

            // Restore defaults
            transform.localScale = startScale;
            if (collider != null) collider.enabled = true;
            if (enemyMovement != null) enemyMovement.enabled = true;

            // Recycle to Pool
            ObjectPool.Instance.ReturnToPool(gameObject, enemyData.enemyName);
        }
    }

    public enum DamageType
    {
        Physical,
        Explosive,
        Elemental
    }
}
