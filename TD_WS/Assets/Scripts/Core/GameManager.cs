using UnityEngine;
using TowerDefense.Utils;
using TowerDefense.Effects;

namespace TowerDefense.Core
{
    public enum GameState
    {
        Playing,
        Paused,
        GameOver
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

        public int CurrentHP => currentHP;
        public int CurrentGold => currentGold;
        public GameState CurrentState => currentState;

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

            Time.timeScale = 1f;
        }

        private void Start()
        {
            ResetGame();
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
            currentHP = startHP;
            currentGold = startGold;
            currentState = GameState.Playing;
            currentWaveIndex = 0;
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
                Time.timeScale = 1f;
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

        public void RestartGame()
        {
            ResetGame();
            EventBus.TriggerGameRestarted();
        }
    }
}
