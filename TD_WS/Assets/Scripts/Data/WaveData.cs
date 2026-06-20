using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Data
{
    [System.Serializable]
    public class EnemySpawnGroup
    {
        public EnemyData enemyData;
        public int count;
        public float spawnInterval = 1f;
    }

    [CreateAssetMenu(fileName = "NewWaveData", menuName = "Tower Defense/Wave Data")]
    public class WaveData : ScriptableObject
    {
        public string waveName;
        public List<EnemySpawnGroup> spawnGroups;
        
        [Header("Scaling")]
        public float healthMultiplier = 1.0f; // Scale enemy health per wave
        public float speedMultiplier = 1.0f;  // Scale enemy speed slightly (optional)
        public int waveBonusGold = 50;
    }
}
