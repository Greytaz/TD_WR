using UnityEngine;

namespace TowerDefense.Data
{
    public enum TowerType
    {
        Archer,
        Cannon,
        Mage
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
        public float range;
        public float fireRate; // Attacks per second
        public float projectileSpeed;
        
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
