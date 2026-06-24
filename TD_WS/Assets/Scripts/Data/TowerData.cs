using UnityEngine;

namespace TowerDefense.Data
{
    public enum TowerType
    {
        Assault,
        Command,
        Energy
    }

    [System.Serializable]
    public class TowerPartData
    {
        public GameObject prefab;
        public int cost;
        
        [Header("Stat Buffs")]
        public float damageBonus;
        public float fireRateMultiplier = 1.0f;
        public float rangeBonus;
        public float critChanceBonus;
    }

    [CreateAssetMenu(fileName = "NewTowerData", menuName = "Tower Defense/Tower Data")]
    public class TowerData : ScriptableObject
    {
        [Header("General")]
        public string towerName;
        public TowerType towerType;
        public Sprite icon;
        public GameObject prefab;

        [Header("Tiers Setup")]
        public TowerTierData tier1;
        public TowerTierData tier2;
        public TowerTierData tier3;

        [Header("Modular Parts Configuration")]
        public TowerPartData[] baseTiers = new TowerPartData[3];   // Tier 1, 2, 3
        public TowerPartData[] bodyTiers = new TowerPartData[3];   // Tier 1, 2, 3
        public TowerPartData[] weaponTiers = new TowerPartData[3]; // Tier 1, 2, 3

        public TowerTierData GetTierData(int tier)
        {
            switch (tier)
            {
                case 1: return tier1;
                case 2: return tier2;
                case 3: return tier3;
                default: return tier1;
            }
        }
    }

    [System.Serializable]
    public class TowerTierData
    {
        public int cost;
        public float damage;
        
        [Header("Damage Setup")]
        public float minDamage;
        public float maxDamage;
        public float critChance; // 0 to 1 (e.g. 0.15f is 15%)
        public float critMultiplier = 2.0f;
        
        public float range;
        public float fireRate; // Attacks per second
        public float projectileSpeed;

        public float GetRandomDamage(out bool isCrit)
        {
            float baseDmg = damage;
            if (minDamage > 0f && maxDamage > 0f)
            {
                baseDmg = Random.Range(minDamage, maxDamage);
            }

            isCrit = false;
            if (critChance > 0f && Random.value <= critChance)
            {
                isCrit = true;
                baseDmg *= critMultiplier;
            }

            return baseDmg;
        }
        
        [Header("Mage/Cannon Special Effects")]
        public float splashRadius;      // For Cannon / Mage splash
        public float slowFactor;        // Mage slow (e.g., 0.5f means slow to 50% speed)
        public float slowDuration;
        public float burnDamagePerSecond;// Mage/Cannon DOT
        public float burnDuration;
        public float stunDuration;       // Mage stun
        public float stunChance;         // Chance to stun
    }
}
