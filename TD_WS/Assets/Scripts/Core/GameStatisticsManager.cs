using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Utils;
using TowerDefense.Data;
using TowerDefense.Towers;

namespace TowerDefense.Core
{
    public class GameStatisticsManager : MonoBehaviour
    {
        public static GameStatisticsManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("GameStatisticsManager");
                Instance = go.AddComponent<GameStatisticsManager>();
                DontDestroyOnLoad(go);
            }
        }

        // Session Stats
        [Serializable]
        public class EnemyStat
        {
            public string enemyName;
            public string enemyType;
            public int count;
        }

        [Serializable]
        public class IndividualTowerStat
        {
            public string towerType;
            public int gridX;
            public int gridZ;
            public int baseTier;
            public int bodyTier;
            public int weaponTier;
            public float damageDealt;
        }

        [Serializable]
        public class TowerTypeStat
        {
            public string towerType;
            public float totalDamageDealt;
        }

        [Serializable]
        public class SessionStatsData
        {
            public string timestamp;
            public int startWave;
            public int highestWaveReached;
            public int wavesCompletedCount;
            public int totalEnemiesKilled;
            public int totalEnemiesPassed;
            public float totalTowerDamageDealt;
            public List<EnemyStat> killedEnemies = new List<EnemyStat>();
            public List<EnemyStat> passedEnemies = new List<EnemyStat>();
            public List<TowerTypeStat> damageByTowerType = new List<TowerTypeStat>();
            public List<IndividualTowerStat> individualTowers = new List<IndividualTowerStat>();
        }

        private SessionStatsData currentSession;
        private bool isSessionActive = false;

        // Runtime dictionary for fast lookups
        private Dictionary<string, int> killedCounts = new Dictionary<string, int>();
        private Dictionary<string, int> passedCounts = new Dictionary<string, int>();
        private Dictionary<TowerType, float> typeDamage = new Dictionary<TowerType, float>();
        private Dictionary<TowerBase, float> individualTowerDamage = new Dictionary<TowerBase, float>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            EventBus.OnWaveStarted += HandleWaveStarted;
            EventBus.OnWaveCompleted += HandleWaveCompleted;
            EventBus.OnEnemyKilledData += HandleEnemyKilled;
            EventBus.OnEnemyReachedBaseData += HandleEnemyReachedBase;
            EventBus.OnTowerDamageDealt += HandleTowerDamageDealt;
            EventBus.OnGameOver += HandleGameOver;
            EventBus.OnGameRestarted += HandleGameRestarted;
        }

        private void OnDisable()
        {
            EventBus.OnWaveStarted -= HandleWaveStarted;
            EventBus.OnWaveCompleted -= HandleWaveCompleted;
            EventBus.OnEnemyKilledData -= HandleEnemyKilled;
            EventBus.OnEnemyReachedBaseData -= HandleEnemyReachedBase;
            EventBus.OnTowerDamageDealt -= HandleTowerDamageDealt;
            EventBus.OnGameOver -= HandleGameOver;
            EventBus.OnGameRestarted -= HandleGameRestarted;
        }

        private void StartNewSession(int startWave)
        {
            currentSession = new SessionStatsData();
            currentSession.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            currentSession.startWave = startWave;
            currentSession.highestWaveReached = startWave;
            currentSession.wavesCompletedCount = 0;

            killedCounts.Clear();
            passedCounts.Clear();
            typeDamage.Clear();
            individualTowerDamage.Clear();

            isSessionActive = true;
            Debug.Log($"[Stats] Started new tracking session at wave {startWave}.");
        }

        private void HandleWaveStarted(int wave)
        {
            if (!isSessionActive)
            {
                StartNewSession(wave);
            }

            if (wave > currentSession.highestWaveReached)
            {
                currentSession.highestWaveReached = wave;
            }
        }

        private void HandleWaveCompleted(int wave)
        {
            if (!isSessionActive) return;
            currentSession.wavesCompletedCount++;
        }

        private void HandleEnemyKilled(EnemyData data)
        {
            if (!isSessionActive || data == null) return;

            string key = data.enemyName ?? "Unknown";
            if (!killedCounts.ContainsKey(key))
            {
                killedCounts[key] = 0;
            }
            killedCounts[key]++;
            currentSession.totalEnemiesKilled++;
        }

        private void HandleEnemyReachedBase(EnemyData data)
        {
            if (!isSessionActive || data == null) return;

            string key = data.enemyName ?? "Unknown";
            if (!passedCounts.ContainsKey(key))
            {
                passedCounts[key] = 0;
            }
            passedCounts[key]++;
            currentSession.totalEnemiesPassed++;
        }

        private void HandleTowerDamageDealt(TowerBase tower, float damage)
        {
            if (!isSessionActive || tower == null) return;

            // 1. Track by tower type
            TowerType type = tower.Data != null ? tower.Data.towerType : TowerType.Assault;
            if (!typeDamage.ContainsKey(type))
            {
                typeDamage[type] = 0f;
            }
            typeDamage[type] += damage;

            // 2. Track by individual tower instance
            if (!individualTowerDamage.ContainsKey(tower))
            {
                individualTowerDamage[tower] = 0f;
            }
            individualTowerDamage[tower] += damage;

            currentSession.totalTowerDamageDealt += damage;
        }

        private void HandleGameOver(int wavesSurvived)
        {
            if (!isSessionActive) return;
            EndAndSaveSession();
        }

        private void HandleGameRestarted()
        {
            if (isSessionActive)
            {
                // Restarting before GameOver is triggered, let's write out previous partial session
                EndAndSaveSession();
            }
            StartNewSession(1);
        }

        private void OnApplicationQuit()
        {
            if (isSessionActive)
            {
                EndAndSaveSession();
            }
        }

        private void EndAndSaveSession()
        {
            if (!isSessionActive) return;
            isSessionActive = false;

            // Compile runtime structures into the serializable data structure
            // Killed
            foreach (var kvp in killedCounts)
            {
                currentSession.killedEnemies.Add(new EnemyStat
                {
                    enemyName = kvp.Key,
                    count = kvp.Value
                });
            }

            // Passed
            foreach (var kvp in passedCounts)
            {
                currentSession.passedEnemies.Add(new EnemyStat
                {
                    enemyName = kvp.Key,
                    count = kvp.Value
                });
            }

            // Damage by type
            foreach (var kvp in typeDamage)
            {
                currentSession.damageByTowerType.Add(new TowerTypeStat
                {
                    towerType = kvp.Key.ToString(),
                    totalDamageDealt = kvp.Value
                });
            }

            // Individual towers
            // Find all towers that exist or existed and track their damage
            foreach (var kvp in individualTowerDamage)
            {
                TowerBase tower = kvp.Key;
                float damage = kvp.Value;

                int gridX = -1;
                int gridZ = -1;

                if (tower != null && GridManager.Instance != null)
                {
                    GridManager.Instance.WorldToGrid(tower.transform.position, out gridX, out gridZ);
                }

                IndividualTowerStat singleStat = new IndividualTowerStat();
                if (tower != null)
                {
                    singleStat.towerType = tower.Data != null ? tower.Data.towerType.ToString() : "Unknown";
                    singleStat.baseTier = tower.BaseTier;
                    singleStat.bodyTier = tower.BodyTier;
                    singleStat.weaponTier = tower.WeaponTier;
                }
                else
                {
                    singleStat.towerType = "DestroyedTower";
                    singleStat.baseTier = 1;
                    singleStat.bodyTier = 1;
                    singleStat.weaponTier = 1;
                }
                singleStat.gridX = gridX;
                singleStat.gridZ = gridZ;
                singleStat.damageDealt = damage;

                currentSession.individualTowers.Add(singleStat);
            }

            // Save JSON to File
            try
            {
                string logsDir = Path.Combine(Application.dataPath, "../Logs");
                if (!Directory.Exists(logsDir))
                {
                    Directory.CreateDirectory(logsDir);
                }

                string fileName = $"game_stats_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string fullPath = Path.Combine(logsDir, fileName);

                string json = JsonUtility.ToJson(currentSession, true);
                File.WriteAllText(fullPath, json);
                Debug.Log($"[Stats] Successfully saved session stats JSON to: {fullPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Stats] Failed to save session stats JSON: {e.Message}");
            }
        }
    }
}