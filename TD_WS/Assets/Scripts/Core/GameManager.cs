using UnityEngine;
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
            // Clear towers from grid
            if (GridManager.Instance != null)
            {
                GridManager.Instance.ClearGrid();
            }

            // Deactivate and return all active enemies to the pool
            var activeEnemies = Object.FindObjectsByType<TowerDefense.Enemies.EnemyBase>(FindObjectsSortMode.None);
            foreach (var enemy in activeEnemies)
            {
                enemy.gameObject.SetActive(false);
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
        }

        private void HandleWaveCompleted(int waveIndex)
        {
            // Interest system: +10% gold at wave end (optional)
            int interest = Mathf.FloorToInt(currentGold * 0.10f);
            
            // Limit interest to maximum of 25 gold per wave for balance
            interest = Mathf.Min(interest, 25);
            
            int waveClearBonus = 50 + (waveIndex * 10);
            
            AddGold(waveClearBonus + interest);
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

            EventBus.TriggerGameOver(currentWaveIndex);
        }

        public void GoToMainMenu()
        {
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
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.SetWaveIndex(0);
            }
        }

        public void ContinueGame()
        {
            int bestWave = SaveSystem.LoadBestWave();
            currentHP = startHP;
            
            // Give them starting gold plus 120 gold for each wave completed, so they can rebuild defenses
            currentGold = startGold + Mathf.Max(0, (bestWave - 1) * 120);
            currentState = GameState.Playing;
            targetTimeScale = 1f;
            Time.timeScale = 1f;

            EventBus.TriggerBaseHPChanged(currentHP);
            EventBus.TriggerGoldChanged(currentGold);

            if (WaveManager.Instance != null)
            {
                // WaveManager's currentWaveIndex will be incremented when the wave starts,
                // so we set it to (bestWave - 1) so that it starts at bestWave.
                WaveManager.Instance.SetWaveIndex(Mathf.Max(0, bestWave - 1));
            }

            currentWaveIndex = bestWave;
            EventBus.TriggerWaveStarted(bestWave);
        }
    }
}
