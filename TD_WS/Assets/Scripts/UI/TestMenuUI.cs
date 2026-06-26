using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerDefense.Core;
using TowerDefense.Enemies;

namespace TowerDefense.UI
{
    public class TestMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button skipWaveButton;
        [SerializeField] private GameObject contentPanel;

        private void Start()
        {
            if (skipWaveButton != null)
            {
                skipWaveButton.onClick.AddListener(OnSkipWaveClicked);
            }
        }

        private void Update()
        {
            // Only show the test menu HUD while playing (not in Main Menu or GameOver)
            if (contentPanel != null && GameManager.Instance != null)
            {
                bool isPlaying = GameManager.Instance.CurrentState == GameState.Playing;
                if (contentPanel.activeSelf != isPlaying)
                {
                    contentPanel.SetActive(isPlaying);
                }
            }
        }

        private void OnSkipWaveClicked()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                return;

            Debug.Log("[TestMenuUI] Skip Wave clicked! Stop spawning, kill active, start next wave.");

            // 1. Stop current wave spawning
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.StopSpawning();
            }

            // 2. Kill all active enemies
            var activeEnemies = new List<EnemyHealth>(EnemyHealth.ActiveEnemies);
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    // Deal massive damage to kill the enemy using standard flow (so gold and events are triggered)
                    enemy.TakeDamage(999999f, DamageType.Physical);
                }
            }

            // 3. Start the next wave immediately
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.StartNextWave();
            }
        }
    }
}