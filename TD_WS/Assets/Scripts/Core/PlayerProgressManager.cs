using UnityEngine;
using TowerDefense.Utils;
using TowerDefense.Data;

namespace TowerDefense.Core
{
    public class PlayerProgressManager : MonoBehaviour
    {
        public static PlayerProgressManager Instance { get; private set; }

        private const string KEY_LEVEL = "PlayerProgress_Level";
        private const string KEY_XP = "PlayerProgress_XP";
        private const string KEY_TOKENS = "PlayerProgress_Tokens";

        private int level = 1;
        private int currentXP = 0;
        private int techTokens = 0;

        public int Level => level;
        public int CurrentXP => currentXP;
        public int TechTokens => techTokens;
        public int BossesKilledInRun => bossesKilledInRun;

        private int bossesKilledInRun = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                LoadProgress();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            EventBus.OnEnemyKilledData += HandleEnemyKilledData;
            EventBus.OnWaveCompleted += HandleWaveCompleted;
            EventBus.OnGameRestarted += HandleGameRestarted;
        }

        private void OnDisable()
        {
            EventBus.OnEnemyKilledData -= HandleEnemyKilledData;
            EventBus.OnWaveCompleted -= HandleWaveCompleted;
            EventBus.OnGameRestarted -= HandleGameRestarted;
        }

        private void HandleEnemyKilledData(EnemyData enemyData)
        {
            if (enemyData != null && enemyData.enemyType == EnemyType.Boss)
            {
                bossesKilledInRun++;
                Debug.Log($"[Progress] Boss killed in run. Count: {bossesKilledInRun}. Adding 50 XP dynamically.");
                AddXP(50);
                GrantBossKillReward();
            }
        }

        private void HandleWaveCompleted(int waveIndex)
        {
            Debug.Log($"[Progress] Wave {waveIndex} completed. Adding 10 XP dynamically.");
            AddXP(10);
        }

        private void HandleGameRestarted()
        {
            bossesKilledInRun = 0;
        }

        public void LoadProgress()
        {
            level = PlayerPrefs.GetInt(KEY_LEVEL, 1);
            currentXP = PlayerPrefs.GetInt(KEY_XP, 0);
            techTokens = PlayerPrefs.GetInt(KEY_TOKENS, 0);
            Debug.Log($"[Progress] Progress Loaded: Level={level}, XP={currentXP}, Tokens={techTokens}");
        }

        public void SaveProgress()
        {
            PlayerPrefs.SetInt(KEY_LEVEL, level);
            PlayerPrefs.SetInt(KEY_XP, currentXP);
            PlayerPrefs.SetInt(KEY_TOKENS, techTokens);
            PlayerPrefs.Save();
            Debug.Log($"[Progress] Progress Saved: Level={level}, XP={currentXP}, Tokens={techTokens}");
        }

        public void ResetProgress()
        {
            level = 1;
            currentXP = 0;
            techTokens = 0;
            SaveProgress();
            EventBus.TriggerPlayerProgressChanged();
            Debug.Log("[Progress] Progress Reset to Level 1, 0 XP, 0 Tokens.");
        }

        public int GetXPRequiredForNextLevel()
        {
            return GetXPRequiredForLevel(level);
        }

        public int GetXPRequiredForLevel(int lvl)
        {
            // Formulas: 100 + (level - 1) * 60 + (level - 1) * (level - 1) * 10
            return 100 + (lvl - 1) * 60 + (lvl - 1) * (lvl - 1) * 10;
        }

        public void AddXP(int amount)
        {
            if (level >= 50)
            {
                currentXP = 0;
                return;
            }

            currentXP += amount;

            while (level < 50 && currentXP >= GetXPRequiredForNextLevel())
            {
                currentXP -= GetXPRequiredForNextLevel();
                level++;
                techTokens++; // +1 techToken per level up
                Debug.Log($"[Progress] Leveled up to {level}! Tech Tokens earned: {techTokens}");
            }

            if (level >= 50)
            {
                currentXP = 0;
            }

            SaveProgress();
            EventBus.TriggerPlayerProgressChanged();
        }

        public void GrantRunXP(int reachedWave, int killedBosses)
        {
            // Formula: earnedXP = reachedWave * 10 + killedBosses * 50
            int earnedXP = reachedWave * 10 + killedBosses * 50;
            Debug.Log($"[Progress] GrantRunXP called with wave={reachedWave}, bosses={killedBosses}. Earned XP: {earnedXP}");
            AddXP(earnedXP);
        }

        public void GrantBossKillReward()
        {
            Debug.Log("[Progress] GrantBossKillReward called on Boss Death.");
            AddTechTokens(1); // baseTechTokensPerBoss = 1
        }

        public void AddTechTokens(int amount)
        {
            techTokens += amount;
            Debug.Log($"[Progress] Tech Tokens added: {amount}. Total: {techTokens}");
            SaveProgress(); // saveAfterReward = true
            EventBus.TriggerPlayerProgressChanged();
        }

        public bool SpendTechTokens(int amount)
        {
            if (techTokens >= amount)
            {
                techTokens -= amount;
                Debug.Log($"[Progress] Spent {amount} Tech Tokens. Remaining: {techTokens}");
                SaveProgress();
                EventBus.TriggerPlayerProgressChanged();
                return true;
            }
            return false;
        }

        public void RefundTechTokens(int amount)
        {
            techTokens += amount;
            Debug.Log($"[Progress] Refunded {amount} Tech Tokens. Total: {techTokens}");
            SaveProgress();
            EventBus.TriggerPlayerProgressChanged();
        }
    }
}
