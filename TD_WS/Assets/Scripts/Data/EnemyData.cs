using UnityEngine;

namespace TowerDefense.Data
{
    public enum EnemyType
    {
        Light,
        Heavy,
        Fast,
        Armored,
        Boss
    }

    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "Tower Defense/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("General")]
        public string enemyName;
        public EnemyType enemyType;
        public GameObject prefab;

        [Header("Stats")]
        public float maxHealth = 100f;
        public float speed = 3f;
        public int goldReward = 15;

        [Header("Damage Resistances (Multiplier: 1 = normal, 0.5 = 50% damage, 2 = double damage)")]
        [Range(0f, 3f)] public float physicalResistance = 1f;  // Archer damage resistance
        [Range(0f, 3f)] public float explosiveResistance = 1f; // Cannon damage resistance
        [Range(0f, 3f)] public float elementalResistance = 1f; // Mage damage resistance
    }
}
