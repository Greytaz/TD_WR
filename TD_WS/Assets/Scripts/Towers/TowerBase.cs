using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Data;
using TowerDefense.Enemies;
using TowerDefense.Utils;
using TowerDefense.Core;
using TowerDefense.Effects;

namespace TowerDefense.Towers
{
    public enum TargetPriority
    {
        First,
        Last,
        Strongest,
        Weakest
    }

    public class TowerBase : MonoBehaviour
    {
        [Header("Data & Configuration")]
        public TowerData towerData;
        public string projectilePoolTag; // e.g. "Arrow", "Cannonball", "MageSphere"

        public Transform rotatableHead;
        public Transform firePoint;
        private TowerVisuals m_ActiveVisuals;

        private int baseTier = 1;
        private int bodyTier = 1;
        private int weaponTier = 1;
        private TowerTierData currentStats;
        private TargetPriority currentPriority = TargetPriority.First;

        private float fireCooldown = 0f;
        private List<EnemyHealth> targetsInRange = new List<EnemyHealth>();
        private EnemyHealth currentTarget;

        public TowerData Data => towerData;
        public int BaseTier => baseTier;
        public int BodyTier => bodyTier;
        public int WeaponTier => weaponTier;
        public int CurrentTier => Mathf.Max(baseTier, Mathf.Max(bodyTier, weaponTier));
        public TowerTierData CurrentStats => currentStats;
        public TargetPriority Priority => currentPriority;

        private void Start()
        {
            UpdateStats();
        }

        private void Update()
        {
            if (currentStats == null) return;

            UpdateTargetsInRange();
            SelectTarget();

            // Rotate head towards target
            if (currentTarget != null && rotatableHead != null)
            {
                Vector3 targetDir = currentTarget.transform.position - rotatableHead.position;
                targetDir.y = 0f; // Lock rotation to Y axis
                if (targetDir.sqrMagnitude > 0.001f)
                {
                    rotatableHead.rotation = Quaternion.Slerp(rotatableHead.rotation, Quaternion.LookRotation(targetDir), Time.deltaTime * 10f);
                }
            }

            // Handle shooting
            if (fireCooldown > 0f)
            {
                fireCooldown -= Time.deltaTime;
            }
            else if (currentTarget != null)
            {
                Shoot();
                fireCooldown = 1f / currentStats.fireRate;
            }
        }

        public void Initialize(TowerData data)
        {
            towerData = data;
            baseTier = 1;
            bodyTier = 1;
            weaponTier = 1;
            UpdateStats();
        }

        public void UpdateStats()
        {
            if (towerData != null)
            {
                currentStats = CompileLiveStats();
                RebuildVisuals();
            }
        }

        public TowerTierData CompileLiveStats()
        {
            if (towerData == null) return null;

            // Start with tier 1 stats as baseline
            TowerTierData stats = new TowerTierData();
            TowerTierData baseline = towerData.tier1;

            if (baseline != null)
            {
                stats.cost = baseline.cost;
                stats.damage = baseline.damage;
                stats.minDamage = baseline.minDamage;
                stats.maxDamage = baseline.maxDamage;
                stats.critChance = baseline.critChance;
                stats.critMultiplier = baseline.critMultiplier;
                stats.range = baseline.range;
                stats.fireRate = baseline.fireRate;
                stats.projectileSpeed = baseline.projectileSpeed;
                stats.splashRadius = baseline.splashRadius;
                stats.slowFactor = baseline.slowFactor;
                stats.slowDuration = baseline.slowDuration;
                stats.burnDamagePerSecond = baseline.burnDamagePerSecond;
                stats.burnDuration = baseline.burnDuration;
                stats.stunDuration = baseline.stunDuration;
                stats.stunChance = baseline.stunChance;
            }

            // Apply Base Part buffs
            if (towerData.baseTiers != null && baseTier > 0 && baseTier <= towerData.baseTiers.Length)
            {
                var p = towerData.baseTiers[baseTier - 1];
                if (p != null)
                {
                    stats.range += p.rangeBonus;
                    stats.critChance += p.critChanceBonus;
                    stats.damage += p.damageBonus;
                    stats.minDamage += p.damageBonus;
                    stats.maxDamage += p.damageBonus;
                    stats.fireRate *= p.fireRateMultiplier;
                }
            }

            // Apply Body Part buffs
            if (towerData.bodyTiers != null && bodyTier > 0 && bodyTier <= towerData.bodyTiers.Length)
            {
                var p = towerData.bodyTiers[bodyTier - 1];
                if (p != null)
                {
                    stats.range += p.rangeBonus;
                    stats.critChance += p.critChanceBonus;
                    stats.damage += p.damageBonus;
                    stats.minDamage += p.damageBonus;
                    stats.maxDamage += p.damageBonus;
                    stats.fireRate *= p.fireRateMultiplier;
                }
            }

            // Apply Weapon Part buffs
            if (towerData.weaponTiers != null && weaponTier > 0 && weaponTier <= towerData.weaponTiers.Length)
            {
                var p = towerData.weaponTiers[weaponTier - 1];
                if (p != null)
                {
                    stats.range += p.rangeBonus;
                    stats.critChance += p.critChanceBonus;
                    stats.damage += p.damageBonus;
                    stats.minDamage += p.damageBonus;
                    stats.maxDamage += p.damageBonus;
                    stats.fireRate *= p.fireRateMultiplier;
                }
            }

            stats.critChance = Mathf.Clamp01(stats.critChance);

            // Apply Run Perks to Tower stats
            if (RunPerkManager.Instance != null)
            {
                RunPerkManager.Instance.ApplyPerkStats(towerData.towerType, stats);
            }

            return stats;
        }

        public Color GetPartColor(int tier)
        {
            if (towerData == null) return Color.white;

            if (towerData.towerType == TowerType.Assault)
            {
                switch (tier)
                {
                    case 1: return new Color(1.0f, 1.0f, 0.62f); // светло-желтый
                    case 2: return new Color(1.0f, 0.85f, 0.0f); // обычный желтый
                    case 3: return new Color(0.55f, 0.40f, 0.0f); // темно-желтый
                    default: return Color.yellow;
                }
            }
            else if (towerData.towerType == TowerType.Energy)
            {
                switch (tier)
                {
                    case 1: return new Color(0.88f, 0.65f, 1.0f); // светло-фиолетовый
                    case 2: return new Color(0.55f, 0.0f, 0.55f); // фиолетовый
                    case 3: return new Color(0.24f, 0.0f, 0.24f); // темно-фиолетовый
                    default: return new Color(0.5f, 0f, 0.5f);
                }
            }
            else if (towerData.towerType == TowerType.Command)
            {
                switch (tier)
                {
                    case 1: return new Color(0.80f, 0.80f, 0.80f); // бледно-серый
                    case 2: return new Color(0.48f, 0.48f, 0.48f); // серый
                    case 3: return new Color(0.12f, 0.12f, 0.12f); // темно-серый почти черный
                    default: return Color.gray;
                }
            }
            return Color.white;
        }

        private GameObject spawnedBase;
        private GameObject spawnedBody;
        private GameObject spawnedWeapon;

        /// <summary>
        /// Метод полностью пересобирает модель башни в бою.
        /// Вызывается при создании башни или при её улучшении.
        /// </summary>
        public void RebuildVisuals()
        {
            // Cache previous rotation if rotatableHead exists
            Quaternion previousRotation = Quaternion.identity;
            bool hasPreviousRotation = false;
            if (rotatableHead != null)
            {
                previousRotation = rotatableHead.localRotation;
                hasPreviousRotation = true;
            }

            // 1. Уничтожаем старые модели
            if (spawnedBase != null) Destroy(spawnedBase);
            if (spawnedBody != null) Destroy(spawnedBody);
            if (spawnedWeapon != null) Destroy(spawnedWeapon);

            // Disable original prefab's built-in renderers to prevent overlapping
            var rootRenderer = GetComponent<MeshRenderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            var origHead = transform.Find("RotatableHead");
            if (origHead != null)
            {
                var headRenderer = origHead.GetComponent<MeshRenderer>();
                if (headRenderer != null) headRenderer.enabled = false;
            }

            // 2. Спавним основание
            GameObject basePrefab = null;
            if (towerData != null && towerData.baseTiers != null && baseTier > 0 && baseTier <= towerData.baseTiers.Length)
            {
                basePrefab = towerData.baseTiers[baseTier - 1]?.prefab;
            }

            if (basePrefab != null)
            {
                spawnedBase = Instantiate(basePrefab, transform);
                spawnedBase.transform.localPosition = Vector3.zero;
                spawnedBase.transform.localRotation = Quaternion.identity;
            }
            else
            {
                spawnedBase = CreateDefaultBaseVisual();
            }

            // 3. Спавним тело
            GameObject bodyPrefab = null;
            if (towerData != null && towerData.bodyTiers != null && bodyTier > 0 && bodyTier <= towerData.bodyTiers.Length)
            {
                bodyPrefab = towerData.bodyTiers[bodyTier - 1]?.prefab;
            }

            if (bodyPrefab != null)
            {
                spawnedBody = Instantiate(bodyPrefab, transform);
                spawnedBody.transform.localPosition = new Vector3(0f, 0.15f, 0f);
                spawnedBody.transform.localRotation = Quaternion.identity;
            }
            else
            {
                spawnedBody = CreateDefaultBodyVisual();
            }

            // 4. Спавним оружие
            GameObject weaponPrefab = null;
            if (towerData != null && towerData.weaponTiers != null && weaponTier > 0 && weaponTier <= towerData.weaponTiers.Length)
            {
                weaponPrefab = towerData.weaponTiers[weaponTier - 1]?.prefab;
            }

            if (weaponPrefab != null)
            {
                spawnedWeapon = Instantiate(weaponPrefab, transform);
                spawnedWeapon.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                spawnedWeapon.transform.localRotation = Quaternion.identity;
            }
            else
            {
                spawnedWeapon = CreateDefaultWeaponVisual();
            }

            // 5. Настраиваем ключевые точки для стрельбы и вращения
            if (spawnedWeapon != null)
            {
                rotatableHead = spawnedWeapon.transform;
                if (hasPreviousRotation)
                {
                    rotatableHead.localRotation = previousRotation;
                }
                Transform fp = spawnedWeapon.transform.Find("FirePoint");
                if (fp == null) fp = spawnedWeapon.transform;
                firePoint = fp;
            }
        }

        private GameObject CreateDefaultBaseVisual()
        {
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "DefaultBase";
            baseObj.transform.SetParent(transform);
            baseObj.transform.localPosition = new Vector3(0f, 0.075f, 0f);
            baseObj.transform.localScale = new Vector3(1.35f, 0.15f, 1.35f);
            
            var col = baseObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var r = baseObj.GetComponent<Renderer>();
            if (r != null)
            {
                Shader pipelineShader = Shader.Find("Universal Render Pipeline/Lit");
                if (pipelineShader == null) pipelineShader = Shader.Find("Lightweight Render Pipeline/Lit");
                if (pipelineShader == null) pipelineShader = Shader.Find("Standard");
                
                Material mat = new Material(pipelineShader);
                mat.color = GetPartColor(baseTier);
                r.sharedMaterial = mat;
            }
            return baseObj;
        }

        private GameObject CreateDefaultBodyVisual()
        {
            GameObject bodyObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bodyObj.name = "DefaultBody";
            bodyObj.transform.SetParent(transform);
            bodyObj.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            bodyObj.transform.localScale = new Vector3(0.6f, 0.4f, 0.6f);
            
            var col = bodyObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var r = bodyObj.GetComponent<Renderer>();
            if (r != null)
            {
                Shader pipelineShader = Shader.Find("Universal Render Pipeline/Lit");
                if (pipelineShader == null) pipelineShader = Shader.Find("Lightweight Render Pipeline/Lit");
                if (pipelineShader == null) pipelineShader = Shader.Find("Standard");

                Material mat = new Material(pipelineShader);
                mat.color = GetPartColor(bodyTier);
                r.sharedMaterial = mat;
            }
            return bodyObj;
        }

        private GameObject CreateDefaultWeaponVisual()
        {
            GameObject weaponObj = new GameObject("DefaultWeaponAssembly");
            weaponObj.transform.SetParent(transform);
            weaponObj.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            weaponObj.transform.localRotation = Quaternion.identity;

            // Main head cube
            GameObject headCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headCube.name = "HeadCube";
            headCube.transform.SetParent(weaponObj.transform);
            headCube.transform.localPosition = Vector3.zero;
            headCube.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
            var col1 = headCube.GetComponent<Collider>();
            if (col1 != null) Destroy(col1);

            // Barrel/Gun pointing forward along Z
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(weaponObj.transform);
            barrel.transform.localPosition = new Vector3(0f, 0f, 0.35f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.15f, 0.3f, 0.15f);
            var col2 = barrel.GetComponent<Collider>();
            if (col2 != null) Destroy(col2);

            // Set materials
            Shader pipelineShader = Shader.Find("Universal Render Pipeline/Lit");
            if (pipelineShader == null) pipelineShader = Shader.Find("Lightweight Render Pipeline/Lit");
            if (pipelineShader == null) pipelineShader = Shader.Find("Standard");

            Material mat = new Material(pipelineShader);
            mat.color = GetPartColor(weaponTier);

            var r1 = headCube.GetComponent<Renderer>();
            if (r1 != null) r1.sharedMaterial = mat;
            var r2 = barrel.GetComponent<Renderer>();
            if (r2 != null) r2.sharedMaterial = mat;

            // Create FirePoint on the tip of the barrel
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(weaponObj.transform);
            fp.transform.localPosition = new Vector3(0f, 0f, 0.7f);
            fp.transform.localRotation = Quaternion.identity;

            return weaponObj;
        }

        public void SetBaseTier(int tier)
        {
            baseTier = tier;
            UpdateStats();
        }

        public void SetBodyTier(int tier)
        {
            bodyTier = tier;
            UpdateStats();
        }

        public void SetWeaponTier(int tier)
        {
            weaponTier = tier;
            UpdateStats();
        }

        public void SetTier(int tier)
        {
            baseTier = tier;
            bodyTier = tier;
            weaponTier = tier;
            UpdateStats();
        }

        public void SetPriority(TargetPriority newPriority)
        {
            currentPriority = newPriority;
        }

        private void UpdateTargetsInRange()
        {
            targetsInRange.Clear();
            if (currentStats == null) return;

            float rangeSqr = currentStats.range * currentStats.range;
            Vector3 myPos = transform.position;

            var activeEnemies = EnemyHealth.ActiveEnemies;
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                EnemyHealth enemy = activeEnemies[i];
                if (enemy != null && enemy.enabled && !enemy.IsDead)
                {
                    float distSqr = (enemy.transform.position - myPos).sqrMagnitude;
                    if (distSqr <= rangeSqr)
                    {
                        targetsInRange.Add(enemy);
                    }
                }
            }
        }

        private void SelectTarget()
        {
            if (targetsInRange.Count == 0)
            {
                currentTarget = null;
                return;
            }

            // Remove any dead targets
            targetsInRange.RemoveAll(t => t == null || !t.gameObject.activeInHierarchy || t.IsDead);

            if (targetsInRange.Count == 0)
            {
                currentTarget = null;
                return;
            }

            // Sort targets based on priority
            switch (currentPriority)
            {
                case TargetPriority.First:
                    // Find enemy with highest distance traversed
                    currentTarget = GetFirstTarget();
                    break;
                case TargetPriority.Last:
                    currentTarget = GetLastTarget();
                    break;
                case TargetPriority.Strongest:
                    currentTarget = GetStrongestTarget();
                    break;
                case TargetPriority.Weakest:
                    currentTarget = GetWeakestTarget();
                    break;
            }
        }

        private EnemyHealth GetFirstTarget()
        {
            EnemyHealth best = null;
            float maxProgress = -999999f;

            foreach (var enemy in targetsInRange)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                
                EnemyMovement move = enemy.GetComponent<EnemyMovement>();
                if (move != null)
                {
                    float progress = move.GetProgressAlongPath();
                    if (progress > maxProgress)
                    {
                        maxProgress = progress;
                        best = enemy;
                    }
                }
            }
            return best ?? targetsInRange[0];
        }

        private EnemyHealth GetLastTarget()
        {
            EnemyHealth best = null;
            float minProgress = 999999f;

            foreach (var enemy in targetsInRange)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                EnemyMovement move = enemy.GetComponent<EnemyMovement>();
                if (move != null)
                {
                    float progress = move.GetProgressAlongPath();
                    if (progress < minProgress)
                    {
                        minProgress = progress;
                        best = enemy;
                    }
                }
            }
            return best ?? targetsInRange[0];
        }

        private EnemyHealth GetStrongestTarget()
        {
            EnemyHealth strongest = null;
            float maxHp = -1f;

            foreach (var enemy in targetsInRange)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                float currentHp = enemy.GetCurrentHealth();
                if (currentHp > maxHp)
                {
                    maxHp = currentHp;
                    strongest = enemy;
                }
            }
            return strongest ?? targetsInRange[0];
        }

        private EnemyHealth GetWeakestTarget()
        {
            EnemyHealth weakest = null;
            float minHp = float.MaxValue;

            foreach (var enemy in targetsInRange)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                float currentHp = enemy.GetCurrentHealth();
                if (currentHp < minHp)
                {
                    minHp = currentHp;
                    weakest = enemy;
                }
            }
            return weakest ?? targetsInRange[0];
        }

        private void Shoot()
        {
            if (currentTarget == null || currentStats == null) return;

            // Запускаем анимацию выстрела на модели башни
            if (m_ActiveVisuals != null)
            {
                m_ActiveVisuals.PlayAttackAnimation();
            }

            // Spawn projectile
            GameObject projObj = ObjectPool.Instance.SpawnFromPool(projectilePoolTag, firePoint.position, firePoint.rotation);
            if (projObj != null)
            {
                Projectiles.ProjectileBase projectile = projObj.GetComponent<Projectiles.ProjectileBase>();
                if (projectile != null)
                {
                    projectile.Initialize(currentTarget, currentStats);
                    projectile.SetSourceTower(this);
                }
            }

            // Screen shake on command fire
            if (towerData.towerType == TowerType.Command && ScreenShake.Instance != null)
            {
                ScreenShake.Instance.Shake(0.15f, 0.05f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (currentStats != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, currentStats.range);
            }
            else if (towerData != null && towerData.tier1 != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, towerData.tier1.range);
            }
        }
    }
}
