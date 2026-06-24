using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerDefense.Data;
using TowerDefense.Towers;
using TowerDefense.Utils;
using TowerDefense.Core;

namespace TowerDefense.UI
{
    public class TowerButton : MonoBehaviour
    {
        [Header("Configuration")]
        public TowerData towerData;

        [Header("UI Elements")]
        public UnityEngine.UI.Image iconImage;
        public TMPro.TextMeshProUGUI costText;
        public UnityEngine.UI.Button buttonComponent;

        private UnityEngine.CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<UnityEngine.CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<UnityEngine.CanvasGroup>();
            }
        }

        private void Start()
        {
            if (buttonComponent == null)
            {
                buttonComponent = GetComponent<UnityEngine.UI.Button>();
            }

            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(OnButtonClicked);
            }

            SetupUI();

            if (GameManager.Instance != null)
            {
                UpdateInteractability(GameManager.Instance.CurrentGold);
            }
        }

        private void OnEnable()
        {
            EventBus.OnGoldChanged += UpdateInteractability;
        }

        private void OnDisable()
        {
            EventBus.OnGoldChanged -= UpdateInteractability;
        }

        private void UpdateInteractability(int currentGold)
        {
            if (towerData == null) return;

            bool canAfford = currentGold >= RunPerkManager.GetBuildCost(towerData);

            if (buttonComponent != null)
            {
                buttonComponent.interactable = canAfford;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = canAfford ? 1.0f : 0.5f;
            }
        }

        private void SetupUI()
        {
            if (towerData == null) return;

            if (iconImage != null && towerData.icon != null)
            {
                iconImage.sprite = towerData.icon;
            }

            if (costText != null)
            {
                costText.text = $"{RunPerkManager.GetBuildCost(towerData)}G";
            }
        }

        private void OnButtonClicked()
        {
            if (towerData != null && TowerPlacement.Instance != null)
            {
                TowerPlacement.Instance.SetPendingTower(towerData);
            }
        }
    }
}
