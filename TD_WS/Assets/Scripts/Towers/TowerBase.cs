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
        public Transform rotatableHead;
        public Transform firePoint;
        public string projectilePoolTag; // e.g. "Arrow", "Cannonball", "MageSphere"

        private int currentTier = 1;
        private TowerTierData currentStats;
        private TargetPriority currentPriority = TargetPriority.First;

        private float fireCooldown = 0f;
        private List<EnemyHealth> targetsInRange = new List<EnemyHealth>();
        private EnemyHealth currentTarget;

        public TowerData Data => towerData;
        public int CurrentTier => currentTier;
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
            currentTier = 1;
            UpdateStats();
        }

        public void UpdateStats()
        {
            if (towerData != null)
            {
                currentStats = towerData.GetTierData(currentTier);
            }
        }

        public void SetTier(int tier)
        {
            currentTier = tier;
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

            // Find all Colliders in range
            Collider[] colliders = Physics.OverlapSphere(transform.position, currentStats.range);
            foreach (var col in colliders)
            {
                EnemyHealth enemy = col.GetComponent<EnemyHealth>();
                if (enemy != null && enemy.enabled)
                {
                    targetsInRange.Add(enemy);
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
            targetsInRange.RemoveAll(t => t == null || !t.gameObject.activeInHierarchy);

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
            float maxDistance = -1f;

            foreach (var enemy in targetsInRange)
            {
                EnemyMovement move = enemy.GetComponent<EnemyMovement>();
                if (move != null)
                {
                    // A simple approximation: check waypoint index or distance to next waypoint
                    // Let's implement a clean way: we can just check distance to the base waypoint.
                    // The lower the distance remaining, the more advanced the enemy is.
                    // Or we can query current waypoint index. Let's make an approximation:
                    // Since enemies move along predefined waypoints, we can measure how close they are to the final waypoint.
                    // But to be precise, let's look at the remaining distance along waypoints.
                    // We can also approximate by checking how far they are from the start point.
                    float distFromStart = Vector3.Distance(enemy.transform.position, GridManager.Instance.GetPathWaypoints()[0]);
                    if (distFromStart > maxDistance)
                    {
                        maxDistance = distFromStart;
                        best = enemy;
                    }
                }
            }
            return best ?? targetsInRange[0];
        }

        private EnemyHealth GetLastTarget()
        {
            EnemyHealth best = null;
            float minDistance = float.MaxValue;

            foreach (var enemy in targetsInRange)
            {
                float distFromStart = Vector3.Distance(enemy.transform.position, GridManager.Instance.GetPathWaypoints()[0]);
                if (distFromStart < minDistance)
                {
                    minDistance = distFromStart;
                    best = enemy;
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

            // Spawn projectile
            GameObject projObj = ObjectPool.Instance.SpawnFromPool(projectilePoolTag, firePoint.position, firePoint.rotation);
            if (projObj != null)
            {
                Projectiles.ProjectileBase projectile = projObj.GetComponent<Projectiles.ProjectileBase>();
                if (projectile != null)
                {
                    projectile.Initialize(currentTarget, currentStats);
                }
            }

            // Screen shake on cannon fire
            if (towerData.towerType == TowerType.Cannon && ScreenShake.Instance != null)
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
