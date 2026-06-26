using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Data;
using TowerDefense.Utils;
using TowerDefense.Enemies;

namespace TowerDefense.Core
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Wave Setup")]
        public List<WaveData> preconfiguredWaves = new List<WaveData>();
        public float timeBetweenWaves = 3f;
        public float waveDifficultyScaling = 1.10f; // +10% enemy health per wave

        [Header("Boss Settings")]
        public EnemyData bossEnemyData;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (bossEnemyData == null)
            {
                bossEnemyData = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/BossEnemy.asset");
            }
        }
#endif

        private int currentWaveIndex = 0;
        private float waveTimer = 0f;
        private bool isSpawning = false;
        private bool isWaveActive = false;
        private int activeEnemyCount = 0;
        private int remainingEnemyCount = 0;

        public int CurrentWaveIndex => currentWaveIndex;
        public float WaveTimer => waveTimer;
        public bool IsWaveActive => isWaveActive;
        public int ActiveEnemyCount => activeEnemyCount;
        public int RemainingEnemyCount => remainingEnemyCount;

        public int GetEnemyCountForWave(int waveIndex)
        {
            if (waveIndex <= 0) return 0;

            // Check if this is a boss wave (every 5th wave)
            if (waveIndex % 5 == 0)
            {
                return bossEnemyData != null ? 1 : 0;
            }

            WaveData wave;
            if (waveIndex - 1 < preconfiguredWaves.Count)
            {
                wave = preconfiguredWaves[waveIndex - 1];
            }
            else
            {
                int templateIndex = (waveIndex - 1) % preconfiguredWaves.Count;
                wave = preconfiguredWaves[templateIndex];
            }

            if (wave == null || wave.spawnGroups == null) return 0;

            int total = 0;
            foreach (var group in wave.spawnGroups)
            {
                total += group.count;
            }
            return total;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            waveTimer = 5f; // First wave starts in 5 seconds
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            if (!isWaveActive)
            {
                waveTimer -= Time.deltaTime;
                if (waveTimer <= 0f)
                {
                    StartNextWave();
                }
            }
        }

        public void StartNextWave()
        {
            if (isWaveActive) return;

            currentWaveIndex++;
            waveTimer = 0f;
            isWaveActive = true;
            isSpawning = true;
            activeEnemyCount = 0;
            remainingEnemyCount = GetEnemyCountForWave(currentWaveIndex);

            EventBus.TriggerWaveStarted(currentWaveIndex);
            StartCoroutine(SpawnWaveCoroutine());
        }

        private IEnumerator SpawnWaveCoroutine()
        {
            List<Vector3> waypoints = GridManager.Instance.GetPathWaypoints();
            if (waypoints == null || waypoints.Count < 2)
            {
                Debug.LogError("Cannot spawn wave: GridManager path waypoints are not configured correctly.");
                isSpawning = false;
                isWaveActive = false;
                yield break;
            }

            WaveData wave;
            float hpMult = Mathf.Pow(waveDifficultyScaling, currentWaveIndex - 1);
            float speedMult = 1f;

            // If we ran out of preconfigured waves, generate procedural waves with scaling stats
            if (currentWaveIndex - 1 < preconfiguredWaves.Count)
            {
                wave = preconfiguredWaves[currentWaveIndex - 1];
                speedMult = wave.speedMultiplier;
            }
            else
            {
                // Procedural generation: pick a random preconfigured wave template and scale its health
                int templateIndex = (currentWaveIndex - 1) % preconfiguredWaves.Count;
                wave = preconfiguredWaves[templateIndex];
                speedMult = wave.speedMultiplier * Mathf.Min(1.5f, 1f + (currentWaveIndex * 0.02f)); // Caps speed scale at +50%
            }

            // Check if this is a boss wave (every 5th wave)
            if (currentWaveIndex % 5 == 0)
            {
                if (bossEnemyData != null)
                {
                    SpawnEnemy(bossEnemyData, waypoints, hpMult, speedMult);
                }
                else
                {
                    Debug.LogWarning("BossEnemyData is not assigned on WaveManager, cannot spawn boss for wave " + currentWaveIndex);
                }
                yield return null;
            }
            else
            {
                // Regular wave: spawn regular enemy groups
                foreach (var group in wave.spawnGroups)
                {
                    for (int i = 0; i < group.count; i++)
                    {
                        if (GameManager.Instance.CurrentState == GameState.GameOver) yield break;

                        SpawnEnemy(group.enemyData, waypoints, hpMult, speedMult);
                        
                        // Spawn in clusters of 3
                        if (i < group.count - 1)
                        {
                            if ((i + 1) % 3 != 0)
                            {
                                // Within a cluster of 3, spawn very close together
                                yield return new WaitForSeconds(0.2f);
                            }
                            else
                            {
                                // Wait between clusters of 3
                                yield return new WaitForSeconds(group.spawnInterval * 3f);
                            }
                        }
                        else
                        {
                            // Last enemy of the group
                            yield return new WaitForSeconds(group.spawnInterval);
                        }
                    }
                }
            }

            isSpawning = false;
            CheckWaveCompletion();
        }

        private void SpawnEnemy(EnemyData data, List<Vector3> waypoints, float hpMult, float speedMult)
        {
            // Spawn from object pool using Enemy Name as tag
            GameObject enemyObj = ObjectPool.Instance.SpawnFromPool(data.enemyName, waypoints[0], Quaternion.identity);
            if (enemyObj != null)
            {
                EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.Spawn(waypoints, hpMult, speedMult);
                    activeEnemyCount++;
                }
            }
        }

        public void RegisterEnemyDeath()
        {
            activeEnemyCount--;
            remainingEnemyCount--;
            if (remainingEnemyCount < 0) remainingEnemyCount = 0;
            CheckWaveCompletion();
        }

        public void StopSpawning()
        {
            StopAllCoroutines();
            isSpawning = false;
            isWaveActive = false;
            activeEnemyCount = 0;
            remainingEnemyCount = 0;
        }

        public void SetWaveIndex(int index)
        {
            StopSpawning();
            currentWaveIndex = index;
            waveTimer = 5f; // give them 5 seconds to build defenses on resume
        }

        private void CheckWaveCompletion()
        {
            if (activeEnemyCount <= 0 && !isSpawning && isWaveActive)
            {
                isWaveActive = false;
                waveTimer = timeBetweenWaves;
                EventBus.TriggerWaveCompleted(currentWaveIndex);
            }
        }

        private void OnEnable()
        {
            EventBus.OnEnemyKilled += HandleEnemyKilled;
            EventBus.OnEnemyReachedBase += HandleEnemyReachedBase;
            EventBus.OnGameRestarted += HandleGameRestarted;
        }

        private void OnDisable()
        {
            EventBus.OnEnemyKilled -= HandleEnemyKilled;
            EventBus.OnEnemyReachedBase -= HandleEnemyReachedBase;
            EventBus.OnGameRestarted -= HandleGameRestarted;
        }

        private void HandleGameRestarted()
        {
            StopAllCoroutines();
            currentWaveIndex = 0;
            waveTimer = 5f; // First wave starts in 5 seconds
            isSpawning = false;
            isWaveActive = false;
            activeEnemyCount = 0;
            remainingEnemyCount = 0;
        }

        private void HandleEnemyKilled(int reward)
        {
            RegisterEnemyDeath();
        }

        private void HandleEnemyReachedBase()
        {
            RegisterEnemyDeath();
        }
    }
}
