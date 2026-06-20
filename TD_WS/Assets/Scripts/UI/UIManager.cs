using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerDefense.Core;
using TowerDefense.Utils;

namespace TowerDefense.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Top HUD Panel")]
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI waveText;
        public TextMeshProUGUI enemyCountText;
        public Button nextWaveButton;

        [Header("Pause Overlay")]
        public GameObject pausePanel;
        public Button resumeButton;
        public Button restartButton;
        public Button quitButton;

        [Header("Game Over Overlay")]
        public GameObject gameOverPanel;
        public TextMeshProUGUI wavesSurvivedText;
        public Button gameOverRestartButton;

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
            // Set up button listeners
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
            if (gameOverRestartButton != null) gameOverRestartButton.onClick.AddListener(OnRestartClicked);
            if (nextWaveButton != null) nextWaveButton.onClick.AddListener(OnNextWaveClicked);

            HideAllOverlays();
        }

        private void OnEnable()
        {
            EventBus.OnGoldChanged += UpdateGoldHUD;
            EventBus.OnBaseHPChanged += UpdateBaseHPHUD;
            EventBus.OnWaveStarted += UpdateWaveHUD;
            EventBus.OnGameOver += ShowGameOverScreen;
            EventBus.OnGameRestarted += HideAllOverlays;
        }

        private void OnDisable()
        {
            EventBus.OnGoldChanged -= UpdateGoldHUD;
            EventBus.OnBaseHPChanged -= UpdateBaseHPHUD;
            EventBus.OnWaveStarted -= UpdateWaveHUD;
            EventBus.OnGameOver -= ShowGameOverScreen;
            EventBus.OnGameRestarted -= HideAllOverlays;
        }

        private void Update()
        {
            bool waveActive = WaveManager.Instance != null && WaveManager.Instance.IsWaveActive;
            bool isPlaying = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing;

            if (nextWaveButton != null)
            {
                nextWaveButton.gameObject.SetActive(isPlaying && !waveActive);
            }

            // Update live enemy counts
            if (enemyCountText != null && WaveManager.Instance != null)
            {
                if (waveActive)
                {
                    enemyCountText.text = $"Enemies: {WaveManager.Instance.ActiveEnemyCount}";
                }
                else
                {
                    enemyCountText.text = $"Next wave in: {WaveManager.Instance.WaveTimer:F1}s";
                }
            }
        }

        private void UpdateGoldHUD(int gold)
        {
            if (goldText != null) goldText.text = $"Gold: {gold}";
        }

        private void UpdateBaseHPHUD(int hp)
        {
            if (hpText != null) hpText.text = $"Lives: {hp}";
        }

        private void UpdateWaveHUD(int wave)
        {
            if (waveText != null) waveText.text = $"Wave: {wave}";
        }

        public void TogglePauseMenu()
        {
            GameManager.Instance.TogglePause();
            if (pausePanel != null)
            {
                pausePanel.SetActive(GameManager.Instance.CurrentState == GameState.Paused);
            }
        }

        private void ShowGameOverScreen(int wavesSurvived)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            if (wavesSurvivedText != null)
            {
                int best = SaveSystem.LoadBestWave();
                wavesSurvivedText.text = $"Waves Survived: {wavesSurvived}\nPersonal Best: {best}";
            }
        }

        private void HideAllOverlays()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        private void OnResumeClicked()
        {
            TogglePauseMenu();
        }

        private void OnRestartClicked()
        {
            HideAllOverlays();
            GameManager.Instance.RestartGame();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnNextWaveClicked()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.StartNextWave();
            }
        }
    }
}
