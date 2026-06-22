using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerDefense.Towers;
using TowerDefense.Utils;
using TowerDefense.Core;

namespace TowerDefense.UI
{
    public class UpgradePanel : MonoBehaviour
    {
        [Header("UI Containers")]
        public GameObject panelContainer;

        [Header("Stats Displays")]
        public TextMeshProUGUI towerNameText;
        public TextMeshProUGUI towerStatsText; // Displays damage, range, fire rate

        [Header("Upgrade & Sell Elements")]
        public Button upgradeButton;
        public TextMeshProUGUI upgradeCostText;
        public Button sellButton;
        public TextMeshProUGUI sellRefundText;

        [Header("Priority Selection")]
        public TMP_Dropdown priorityDropdown;

        private TowerBase selectedTower;
        private UnityEngine.CanvasGroup upgradeButtonCanvasGroup;

        private void Start()
        {
            if (upgradeButton != null)
            {
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
                upgradeButtonCanvasGroup = upgradeButton.GetComponent<UnityEngine.CanvasGroup>();
                if (upgradeButtonCanvasGroup == null)
                {
                    upgradeButtonCanvasGroup = upgradeButton.gameObject.AddComponent<UnityEngine.CanvasGroup>();
                }
            }
            if (sellButton != null) sellButton.onClick.AddListener(OnSellClicked);
            
            if (priorityDropdown != null)
            {
                priorityDropdown.onValueChanged.AddListener(OnPriorityChanged);
                
                // Populate dropdown options
                priorityDropdown.ClearOptions();
                var options = new System.Collections.Generic.List<string> { "First", "Last", "Strongest", "Weakest" };
                priorityDropdown.AddOptions(options);
            }

            HidePanel();
        }

        private void OnEnable()
        {
            EventBus.OnTowerSelected += ShowPanel;
            EventBus.OnTowerDeselected += HidePanel;
            EventBus.OnGoldChanged += HandleGoldChanged;
        }

        private void OnDisable()
        {
            EventBus.OnTowerSelected -= ShowPanel;
            EventBus.OnTowerDeselected -= HidePanel;
            EventBus.OnGoldChanged -= HandleGoldChanged;
        }

        private void ShowPanel(TowerBase tower)
        {
            selectedTower = tower;
            if (selectedTower == null)
            {
                HidePanel();
                return;
            }

            if (panelContainer != null)
            {
                panelContainer.SetActive(true);
            }

            UpdateStatsUI();
        }

        private void HidePanel()
        {
            selectedTower = null;
            if (panelContainer != null)
            {
                panelContainer.SetActive(false);
            }
        }

        private void UpdateStatsUI()
        {
            if (selectedTower == null) return;

            // Update Text
            if (towerNameText != null)
            {
                towerNameText.text = $"{selectedTower.Data.towerName} (Tier {selectedTower.CurrentTier})";
            }

            if (towerStatsText != null)
            {
                var stats = selectedTower.CurrentStats;
                string damageText = (stats.minDamage > 0f && stats.maxDamage > 0f) ? $"{stats.minDamage:F0}-{stats.maxDamage:F0}" : stats.damage.ToString("F0");
                if (stats.critChance > 0f)
                {
                    damageText += $" ({stats.critChance * 100f:F0}% Crit)";
                }
                towerStatsText.text = $"Dmg: {damageText}\nRange: {stats.range}\nSpd: {stats.fireRate}/s";
            }

            // Upgrade Button Setup
            if (selectedTower.CurrentTier >= 3)
            {
                if (upgradeButton != null) upgradeButton.interactable = false;
                if (upgradeCostText != null) upgradeCostText.text = "MAX TIER";
                if (upgradeButtonCanvasGroup != null) upgradeButtonCanvasGroup.alpha = 0.5f;
            }
            else
            {
                int nextTier = selectedTower.CurrentTier + 1;
                int cost = selectedTower.Data.GetTierData(nextTier).cost;
                
                if (upgradeCostText != null) upgradeCostText.text = $"Upgrade: {cost}G";
                
                bool canAfford = GameManager.Instance.CurrentGold >= cost;
                if (upgradeButton != null)
                {
                    upgradeButton.interactable = canAfford;
                }
                if (upgradeButtonCanvasGroup != null)
                {
                    upgradeButtonCanvasGroup.alpha = canAfford ? 1.0f : 0.5f;
                }
            }

            // Sell Button Setup
            int totalSpent = selectedTower.Data.tier1.cost;
            if (selectedTower.CurrentTier >= 2) totalSpent += selectedTower.Data.tier2.cost;
            if (selectedTower.CurrentTier >= 3) totalSpent += selectedTower.Data.tier3.cost;
            int refund = Mathf.FloorToInt(totalSpent * 0.70f);

            if (sellRefundText != null) sellRefundText.text = $"Sell: +{refund}G";

            // Priority Dropdown Setup
            if (priorityDropdown != null)
            {
                priorityDropdown.SetValueWithoutNotify((int)selectedTower.Priority);
            }
        }

        private void HandleGoldChanged(int currentGold)
        {
            if (selectedTower != null)
            {
                UpdateStatsUI();
            }
        }

        private void OnUpgradeClicked()
        {
            if (selectedTower != null && TowerUpgrade.Instance != null)
            {
                if (TowerUpgrade.Instance.UpgradeTower(selectedTower))
                {
                    UpdateStatsUI();
                }
            }
        }

        private void OnSellClicked()
        {
            if (selectedTower != null && TowerUpgrade.Instance != null)
            {
                TowerUpgrade.Instance.SellTower(selectedTower);
            }
        }

        private void OnPriorityChanged(int value)
        {
            if (selectedTower != null)
            {
                selectedTower.SetPriority((TargetPriority)value);
            }
        }
    }
}
