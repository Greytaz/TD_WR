using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerDefense.Data;
using TowerDefense.Towers;

namespace TowerDefense.UI
{
    public class TowerButton : MonoBehaviour
    {
        [Header("Configuration")]
        public TowerData towerData;

        [Header("UI Elements")]
        public Image iconImage;
        public TextMeshProUGUI costText;
        public Button buttonComponent;

        private void Start()
        {
            if (buttonComponent == null)
            {
                buttonComponent = GetComponent<Button>();
            }

            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(OnButtonClicked);
            }

            SetupUI();
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
                costText.text = $"{towerData.tier1.cost}G";
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
