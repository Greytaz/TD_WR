using UnityEngine;
using TMPro;
using TowerDefense.Core;
using TowerDefense.Utils;

namespace TowerDefense.UI
{
    [DefaultExecutionOrder(100)]
    public class PlayerProgressUI : MonoBehaviour
    {
        private TextMeshProUGUI progressText;

        private void Start()
        {
            InitializeUI();
            UpdateUI();
            EventBus.OnPlayerProgressChanged += UpdateUI;
        }

        private void OnDestroy()
        {
            EventBus.OnPlayerProgressChanged -= UpdateUI;
        }

        private void InitializeUI()
        {
            // If already created, find it
            Transform existing = transform.Find("PlayerProgressText");
            if (existing != null)
            {
                progressText = existing.GetComponent<TextMeshProUGUI>();
                return;
            }

            // Create new text GameObject
            GameObject textObj = new GameObject("PlayerProgressText", typeof(RectTransform));
            textObj.transform.SetParent(transform, false);

            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.52f);
            rt.anchorMax = new Vector2(0.5f, 0.52f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(400f, 75f);

            progressText = textObj.AddComponent<TextMeshProUGUI>();

            // Copy styling from BestWaveText if available
            Transform bestWaveTextObj = transform.Find("BestWaveText");
            if (bestWaveTextObj != null)
            {
                TextMeshProUGUI source = bestWaveTextObj.GetComponent<TextMeshProUGUI>();
                if (source != null)
                {
                    progressText.font = source.font;
                    progressText.fontSize = source.fontSize;
                    progressText.color = source.color;
                    progressText.alignment = TextAlignmentOptions.Center;
                    progressText.lineSpacing = source.lineSpacing;
                }
            }
            else
            {
                progressText.fontSize = 20f;
                progressText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                progressText.alignment = TextAlignmentOptions.Center;
            }
        }

        public void UpdateUI()
        {
            if (progressText == null)
            {
                InitializeUI();
            }

            if (progressText == null || PlayerProgressManager.Instance == null) return;

            int lvl = PlayerProgressManager.Instance.Level;
            int xp = PlayerProgressManager.Instance.CurrentXP;
            int reqXp = PlayerProgressManager.Instance.GetXPRequiredForNextLevel();
            int tokens = PlayerProgressManager.Instance.TechTokens;

            progressText.text = $"Level: {lvl}\nXP: {xp} / {reqXp}\nTokens: {tokens}";
        }
    }
}
