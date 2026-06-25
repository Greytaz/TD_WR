using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
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
        public Button speedButton;
        public TextMeshProUGUI speedText;
        public Button hudMenuButton;

        [Header("Pause Overlay")]
        public GameObject pausePanel;
        public Button resumeButton;
        public Button restartButton;
        public Button quitButton;
        public Button mainMenuButton;

        [Header("Game Over Overlay")]
        public GameObject gameOverPanel;
        public TextMeshProUGUI wavesSurvivedText;
        public Button gameOverRestartButton;
        public Button gameOverMainMenuButton;
        public Button gameOverQuitButton;

        [Header("Main Menu Overlay")]
        public GameObject mainMenuPanel;
        public Button startButton;
        public Button continueButton;
        public TextMeshProUGUI bestWaveText;

        [Header("Player Progress HUD")]
        public TextMeshProUGUI hudProgressText;
        public TextMeshProUGUI hudTokensText;

        private RectTransform xpFillRect;

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
            if (speedButton != null) speedButton.onClick.AddListener(OnSpeedButtonClicked);
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
            if (hudMenuButton != null) hudMenuButton.onClick.AddListener(OnHUDMenuClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            if (gameOverMainMenuButton != null) gameOverMainMenuButton.onClick.AddListener(OnMainMenuClicked);
            if (gameOverQuitButton != null) gameOverQuitButton.onClick.AddListener(OnQuitClicked);

            HideAllOverlays();
            UpdateSpeedHUD();

            if (GameManager.Instance != null)
            {
                UpdateGoldHUD(GameManager.Instance.CurrentState == GameState.MainMenu ? GameManager.Instance.startGold : GameManager.Instance.CurrentGold);
                UpdateBaseHPHUD(GameManager.Instance.CurrentState == GameState.MainMenu ? GameManager.Instance.startHP : GameManager.Instance.CurrentHP);
                UpdateWaveHUD(Mathf.Max(1, WaveManager.Instance != null ? WaveManager.Instance.CurrentWaveIndex : 1));
            }

            // Enable MainMenuPanel on start
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
                
                int bestWave = SaveSystem.LoadBestWave();
                int currentRunWave = SaveSystem.LoadCurrentRunWave();
                
                // Set continue button interactable if a current run wave save exists
                if (continueButton != null)
                {
                    continueButton.interactable = (currentRunWave > 0);
                }

                // Show best wave achieved
                if (bestWaveText != null)
                {
                    if (bestWave > 0)
                    {
                        bestWaveText.text = $"Best Wave: {bestWave}";
                        bestWaveText.gameObject.SetActive(true);
                    }
                    else
                    {
                        bestWaveText.gameObject.SetActive(false);
                    }
                }
            }

            UpdateProgressHUD();
        }

        private void UpdateProgressHUD()
        {
            if (hudProgressText != null && PlayerProgressManager.Instance != null)
            {
                int lvl = PlayerProgressManager.Instance.Level;
                int xp = PlayerProgressManager.Instance.CurrentXP;
                int reqXp = PlayerProgressManager.Instance.GetXPRequiredForNextLevel();
                int tokens = PlayerProgressManager.Instance.TechTokens;
                hudProgressText.text = $"lvl {lvl} xp {xp}/{reqXp}";

                if (hudTokensText != null)
                {
                    hudTokensText.text = $"Tokens: {tokens}";
                }

                if (xpFillRect == null)
                {
                    Transform fillTrans = hudProgressText.transform.Find("XPProgressBar/XPProgressFill");
                    if (fillTrans != null)
                    {
                        xpFillRect = fillTrans.GetComponent<RectTransform>();
                    }
                }

                if (xpFillRect != null)
                {
                    float fill = reqXp > 0 ? Mathf.Clamp01((float)xp / reqXp) : 0f;
                    xpFillRect.anchorMax = new Vector2(fill, 1f);
                    xpFillRect.offsetMax = Vector2.zero; // Make sure right offset is reset to prevent deformation
                }
            }
        }

        private void OnEnable()
        {
            EventBus.OnGoldChanged += UpdateGoldHUD;
            EventBus.OnBaseHPChanged += UpdateBaseHPHUD;
            EventBus.OnWaveStarted += UpdateWaveHUD;
            EventBus.OnGameOver += ShowGameOverScreen;
            EventBus.OnGameRestarted += HideAllOverlays;
            EventBus.OnGameRestarted += UpdateSpeedHUD;
            EventBus.OnPlayerProgressChanged += UpdateProgressHUD;
        }

        private void OnDisable()
        {
            EventBus.OnGoldChanged -= UpdateGoldHUD;
            EventBus.OnBaseHPChanged -= UpdateBaseHPHUD;
            EventBus.OnWaveStarted -= UpdateWaveHUD;
            EventBus.OnGameOver -= ShowGameOverScreen;
            EventBus.OnGameRestarted -= HideAllOverlays;
            EventBus.OnGameRestarted -= UpdateSpeedHUD;
            EventBus.OnPlayerProgressChanged -= UpdateProgressHUD;
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
                    enemyCountText.text = $"Enemies: {WaveManager.Instance.RemainingEnemyCount}";
                }
                else
                {
                    int incomingCount = WaveManager.Instance.GetEnemyCountForWave(WaveManager.Instance.CurrentWaveIndex + 1);
                    enemyCountText.text = $"Enemies: {incomingCount} (Next: {WaveManager.Instance.WaveTimer:F1}s)";
                }
            }

            // Handle Escape key to pause/unpause
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (PerkChoiceUI.Instance != null && PerkChoiceUI.Instance.IsActive) return;

                if (GameManager.Instance != null && 
                    (GameManager.Instance.CurrentState == GameState.Playing || GameManager.Instance.CurrentState == GameState.Paused))
                {
                    TogglePauseMenu();
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (PerkChoiceUI.Instance != null && PerkChoiceUI.Instance.IsActive) return;

            // Pause the game if we lose focus and are currently playing
            if (!hasFocus && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                TogglePauseMenu();
            }
        }

        private void UpdateGoldHUD(int gold)
        {
            if (goldText != null) goldText.text = $"Gold: {gold}G";
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
            if (PerkChoiceUI.Instance != null && PerkChoiceUI.Instance.IsActive) return;

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
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        }

        private void OnStartClicked()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            GameManager.Instance.StartNewGame();
        }

        private void OnContinueClicked()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            GameManager.Instance.ContinueGame();
        }

        private void OnResumeClicked()
        {
            TogglePauseMenu();
        }

        private void OnHUDMenuClicked()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                TogglePauseMenu();
            }
        }

        private void OnMainMenuClicked()
        {
            HideAllOverlays();
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
                
                int bestWave = SaveSystem.LoadBestWave();
                if (continueButton != null)
                {
                    continueButton.interactable = SaveSystem.HasActiveRun();
                }
                if (bestWaveText != null)
                {
                    if (bestWave > 0)
                    {
                        bestWaveText.text = $"Best Wave: {bestWave}";
                        bestWaveText.gameObject.SetActive(true);
                    }
                    else
                    {
                        bestWaveText.gameObject.SetActive(false);
                    }
                }
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GoToMainMenu();
            }
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

        private void OnSpeedButtonClicked()
        {
            if (GameManager.Instance == null) return;

            float currentSpeed = GameManager.Instance.TargetTimeScale;
            float newSpeed = (currentSpeed == 1f) ? 2f : 1f;

            GameManager.Instance.SetGameSpeed(newSpeed);
            UpdateSpeedHUD();
        }

        public void UpdateSpeedHUD()
        {
            if (speedText != null && GameManager.Instance != null)
            {
                speedText.text = GameManager.Instance.TargetTimeScale >= 2f ? "2x" : "1x";
            }
        }
    }
}
