using UnityEngine;
using System.Collections.Generic;
using TowerDefense.Utils;
using TowerDefense.Effects;

namespace TowerDefense.Core
{
    public enum GameState
    {
        Playing,
        Paused,
        GameOver,
        MainMenu
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Starting Parameters")]
        public int startHP = 20;
        public int startGold = 150;

        private int currentHP;
        private int currentGold;
        private GameState currentState = GameState.Playing;
        private int currentWaveIndex = 0;
        private float targetTimeScale = 1f;

        public int CurrentHP => currentHP;
        public int CurrentGold => currentGold;
        public GameState CurrentState => currentState;
        public float TargetTimeScale => targetTimeScale;

        public void SetGameSpeed(float speed)
        {
            targetTimeScale = speed;
            if (currentState == GameState.Playing)
            {
                Time.timeScale = targetTimeScale;
            }
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

            targetTimeScale = 1f;
            Time.timeScale = 0f; // Start paused for Main Menu
            currentState = GameState.MainMenu;
        }

        private void Start()
        {
            // Do not call ResetGame automatically. Wait for UI interactions.
            currentState = GameState.MainMenu;
            Time.timeScale = 0f;
        }

        private void OnEnable()
        {
            EventBus.OnEnemyReachedBase += HandleEnemyReachedBase;
            EventBus.OnEnemyKilled += HandleEnemyKilled;
            EventBus.OnWaveStarted += HandleWaveStarted;
            EventBus.OnWaveCompleted += HandleWaveCompleted;
        }

        private void OnDisable()
        {
            EventBus.OnEnemyReachedBase -= HandleEnemyReachedBase;
            EventBus.OnEnemyKilled -= HandleEnemyKilled;
            EventBus.OnWaveStarted -= HandleWaveStarted;
            EventBus.OnWaveCompleted -= HandleWaveCompleted;
        }

        private void ResetGame()
        {
            // Clear towers from grid and generate a new procedural path
            if (GridManager.Instance != null)
            {
                GridManager.Instance.GenerateProceduralPath();
            }

            // Deactivate and return all active enemies to the pool
            var activeEnemies = Object.FindObjectsByType<TowerDefense.Enemies.EnemyBase>(FindObjectsSortMode.None);
            foreach (var enemy in activeEnemies)
            {
                enemy.gameObject.SetActive(false);
            }

            // Reset current run perks
            if (RunPerkManager.Instance != null)
            {
                RunPerkManager.Instance.ResetRunPerks();
            }

            currentHP = startHP;
            currentGold = startGold;
            currentState = GameState.Playing;
            currentWaveIndex = 0;
            targetTimeScale = 1f;
            Time.timeScale = 1f;

            EventBus.TriggerBaseHPChanged(currentHP);
            EventBus.TriggerGoldChanged(currentGold);
        }

        private void HandleEnemyReachedBase()
        {
            if (currentState != GameState.Playing) return;

            currentHP--;
            EventBus.TriggerBaseHPChanged(currentHP);

            // Screen Shake on damage!
            if (ScreenShake.Instance != null)
            {
                ScreenShake.Instance.Shake(0.3f, 0.15f);
            }

            if (currentHP <= 0)
            {
                GameOver();
            }
        }

        private void HandleEnemyKilled(int goldReward)
        {
            if (currentState != GameState.Playing) return;

            AddGold(goldReward);
        }

        private void HandleWaveStarted(int waveIndex)
        {
            currentWaveIndex = waveIndex;
            SaveSystem.SaveCurrentRunWave(waveIndex);
        }

        private void HandleWaveCompleted(int waveIndex)
        {
            // Interest system: +10% gold at wave end (optional)
            int interest = Mathf.FloorToInt(currentGold * 0.10f);
            
            // Limit interest to maximum of 25 gold per wave for balance
            interest = Mathf.Min(interest, 25);
            
            int waveClearBonus = 50 + (waveIndex * 10);
            
            AddGold(waveClearBonus + interest);

            // Trigger active run autosave (saves HP, Gold, Wave, Perks, and Towers)
            AutoSaveActiveRun();
        }

        private void AutoSaveActiveRun()
        {
            if (currentState != GameState.Playing && currentState != GameState.Paused) return;

            SaveSystem.ActiveRunSaveData data = new SaveSystem.ActiveRunSaveData();
            data.gold = currentGold;
            data.hp = currentHP;
            data.wave = currentWaveIndex;

            if (RunPerkManager.Instance != null)
            {
                data.activePerks = new List<string>(RunPerkManager.Instance.ActiveRunPerks);
            }

            // Save all towers from the grid
            if (GridManager.Instance != null)
            {
                for (int x = 0; x < GridManager.Instance.gridWidth; x++)
                {
                    for (int z = 0; z < GridManager.Instance.gridHeight; z++)
                    {
                        var tower = GridManager.Instance.GetTowerAtCell(x, z);
                        if (tower != null)
                        {
                            SaveSystem.TowerSaveEntry entry = new SaveSystem.TowerSaveEntry();
                            entry.x = x;
                            entry.z = z;
                            entry.towerType = tower.Data.towerType.ToString();
                            entry.baseTier = tower.BaseTier;
                            entry.bodyTier = tower.BodyTier;
                            entry.weaponTier = tower.WeaponTier;
                            data.towers.Add(entry);
                        }
                    }
                }

                // Save procedural path cells to preserve the active run layout
                foreach (var cell in GridManager.Instance.GetOrderedPathCells())
                {
                    SaveSystem.PathCellSaveEntry entry = new SaveSystem.PathCellSaveEntry();
                    entry.x = cell.x;
                    entry.z = cell.y;
                    data.pathCells.Add(entry);
                }
            }

            SaveSystem.SaveActiveRun(data);
        }

        public void AddGold(int amount)
        {
            currentGold += amount;
            EventBus.TriggerGoldChanged(currentGold);
        }

        public bool SpendGold(int amount)
        {
            if (currentGold >= amount)
            {
                currentGold -= amount;
                EventBus.TriggerGoldChanged(currentGold);
                return true;
            }
            return false;
        }

        public void TogglePause()
        {
            if (currentState == GameState.GameOver) return;

            if (currentState == GameState.Paused)
            {
                currentState = GameState.Playing;
                Time.timeScale = targetTimeScale;
            }
            else
            {
                currentState = GameState.Paused;
                Time.timeScale = 0f;
            }
        }

        public void GameOver()
        {
            currentState = GameState.GameOver;
            Time.timeScale = 0f;

            // Save High Score
            SaveSystem.SaveBestWave(currentWaveIndex);
            SaveSystem.SaveCurrentRunWave(0); // Clear current run wave because the run is over
            SaveSystem.ClearActiveRun(); // Clear active run autosave because the run is over

            EventBus.TriggerGameOver(currentWaveIndex);
        }

        public void GoToMainMenu()
        {
            // AutoSave before returning to main menu (ensures changes in preparation phase or pause menu are saved)
            AutoSaveActiveRun();

            currentState = GameState.MainMenu;
            Time.timeScale = 0f;
        }

        public void RestartGame()
        {
            ResetGame();
            EventBus.TriggerGameRestarted();
        }

        public void StartNewGame()
        {
            ResetGame();
            SaveSystem.SaveCurrentRunWave(1);
            SaveSystem.ClearActiveRun(); // Clear previous active run save
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.SetWaveIndex(0);
            }
        }

        private Data.TowerData GetTowerDataByType(string typeStr)
        {
            var buttons = Object.FindObjectsByType<UI.TowerButton>(FindObjectsSortMode.None);
            foreach (var btn in buttons)
            {
                if (btn.towerData != null && btn.towerData.towerType.ToString() == typeStr)
                {
                    return btn.towerData;
                }
            }
            return null;
        }

        public void ContinueGame()
        {
            if (SaveSystem.HasActiveRun())
            {
                var data = SaveSystem.LoadActiveRun();
                if (data != null)
                {
                    currentHP = data.hp;
                    currentGold = data.gold;
                    currentWaveIndex = data.wave;

                    currentState = GameState.Playing;
                    targetTimeScale = 1f;
                    Time.timeScale = 1f;

                    EventBus.TriggerBaseHPChanged(currentHP);
                    EventBus.TriggerGoldChanged(currentGold);

                    // Rebuild active perks state
                    if (RunPerkManager.Instance != null)
                    {
                        RunPerkManager.Instance.LoadActiveRunPerks(data.activePerks);
                    }

                    // Restore procedural path from saved data to preserve active run layout
                    if (GridManager.Instance != null && data.pathCells != null && data.pathCells.Count > 0)
                    {
                        List<Vector2Int> loadedPath = new List<Vector2Int>();
                        foreach (var entry in data.pathCells)
                        {
                            loadedPath.Add(new Vector2Int(entry.x, entry.z));
                        }
                        GridManager.Instance.RestoreProceduralPath(loadedPath);
                    }

                    // Rebuild grid towers
                    if (GridManager.Instance != null)
                    {
                        // ClearGrid is handled inside RestoreProceduralPath, but let's make sure it's clear
                        // if we didn't have path data.
                        if (data.pathCells == null || data.pathCells.Count == 0)
                        {
                            GridManager.Instance.ClearGrid();
                        }

                        foreach (var tSave in data.towers)
                        {
                            Data.TowerData tData = GetTowerDataByType(tSave.towerType);
                            if (tData != null)
                            {
                                GameObject prefabToSpawn = tData.prefab;
                                if (prefabToSpawn == null)
                                {
                                    // Fallback to loading the standard tower prefab from assets if not assigned in tests
                                    #if UNITY_EDITOR
                                    prefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Towers/{tSave.towerType}Tower.prefab");
                                    #endif
                                }

                                if (prefabToSpawn != null)
                                {
                                    GameObject towerObj = Instantiate(prefabToSpawn);
                                    Towers.TowerBase towerInstance = towerObj.GetComponent<Towers.TowerBase>();
                                    towerInstance.Initialize(tData);
                                    towerInstance.SetBaseTier(tSave.baseTier);
                                    towerInstance.SetBodyTier(tSave.bodyTier);
                                    towerInstance.SetWeaponTier(tSave.weaponTier);

                                    GridManager.Instance.PlaceTower(tSave.x, tSave.z, towerInstance);
                                }
                                else
                                {
                                    Debug.LogError($"[Autosave] Prefab for {tSave.towerType} is null, cannot restore tower on grid!");
                                }
                            }
                        }
                    }

                    if (WaveManager.Instance != null)
                    {
                        // WaveManager's currentWaveIndex will be incremented when the wave starts,
                        // so we set it to (currentWaveIndex - 1) so that it starts at currentWaveIndex.
                        WaveManager.Instance.SetWaveIndex(Mathf.Max(0, currentWaveIndex - 1));
                    }

                    EventBus.TriggerWaveStarted(currentWaveIndex);
                    return;
                }
            }

            // Fallback old behavior
            int savedWave = SaveSystem.LoadCurrentRunWave();
            if (savedWave <= 0)
            {
                savedWave = 1;
            }
            currentHP = startHP;
            
            // Give them starting gold plus 120 gold for each wave completed, so they can rebuild defenses
            currentGold = startGold + Mathf.Max(0, (savedWave - 1) * 120);
            currentState = GameState.Playing;
            targetTimeScale = 1f;
            Time.timeScale = 1f;

            EventBus.TriggerBaseHPChanged(currentHP);
            EventBus.TriggerGoldChanged(currentGold);

            if (WaveManager.Instance != null)
            {
                // WaveManager's currentWaveIndex will be incremented when the wave starts,
                // so we set it to (savedWave - 1) so that it starts at savedWave.
                WaveManager.Instance.SetWaveIndex(Mathf.Max(0, savedWave - 1));
            }

            currentWaveIndex = savedWave;
            EventBus.TriggerWaveStarted(savedWave);
        }
    }
}
