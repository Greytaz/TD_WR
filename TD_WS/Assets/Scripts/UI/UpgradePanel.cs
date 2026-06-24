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

        // Modular Buttons cloned at runtime from upgradeButton
        private Button baseUpgradeBtn;
        private TextMeshProUGUI baseCostTxt;
        private Button bodyUpgradeBtn;
        private TextMeshProUGUI bodyCostTxt;
        private Button weaponUpgradeBtn;
        private TextMeshProUGUI weaponCostTxt;

        private void Start()
        {
            // Dynamically clone and reposition the upgrade buttons to support 3 modular parts
            if (upgradeButton != null)
            {
                upgradeButton.gameObject.SetActive(false); // Hide original single upgrade button

                // Clone Weapon Upgrade Button
                weaponUpgradeBtn = Instantiate(upgradeButton, upgradeButton.transform.parent);
                weaponUpgradeBtn.name = "UpgradeWeaponButton";
                weaponUpgradeBtn.gameObject.SetActive(true);
                weaponUpgradeBtn.onClick.AddListener(OnUpgradeWeaponClicked);
                weaponCostTxt = weaponUpgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
                SetAnchor(weaponUpgradeBtn.GetComponent<RectTransform>(), 0.54f);

                // Clone Body Upgrade Button
                bodyUpgradeBtn = Instantiate(upgradeButton, upgradeButton.transform.parent);
                bodyUpgradeBtn.name = "UpgradeBodyButton";
                bodyUpgradeBtn.gameObject.SetActive(true);
                bodyUpgradeBtn.onClick.AddListener(OnUpgradeBodyClicked);
                bodyCostTxt = bodyUpgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
                SetAnchor(bodyUpgradeBtn.GetComponent<RectTransform>(), 0.44f);

                // Clone Base Upgrade Button
                baseUpgradeBtn = Instantiate(upgradeButton, upgradeButton.transform.parent);
                baseUpgradeBtn.name = "UpgradeBaseButton";
                baseUpgradeBtn.gameObject.SetActive(true);
                baseUpgradeBtn.onClick.AddListener(OnUpgradeBaseClicked);
                baseCostTxt = baseUpgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
                SetAnchor(baseUpgradeBtn.GetComponent<RectTransform>(), 0.34f);
            }

            if (sellButton != null)
            {
                sellButton.onClick.AddListener(OnSellClicked);
                SetAnchor(sellButton.GetComponent<RectTransform>(), 0.24f);
            }

            if (priorityDropdown != null)
            {
                priorityDropdown.onValueChanged.AddListener(OnPriorityChanged);
                SetAnchor(priorityDropdown.GetComponent<RectTransform>(), 0.14f);

                priorityDropdown.ClearOptions();
                var options = new System.Collections.Generic.List<string> { "First", "Last", "Strongest", "Weakest" };
                priorityDropdown.AddOptions(options);
            }

            HidePanel();
        }

        private void SetAnchor(RectTransform rt, float yAnchor)
        {
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, yAnchor);
                rt.anchorMax = new Vector2(0.5f, yAnchor);
                rt.anchoredPosition = Vector2.zero;
            }
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
                towerNameText.text = $"{selectedTower.Data.towerName}";
            }

            if (towerStatsText != null)
            {
                var stats = selectedTower.CurrentStats;
                string damageText = (stats.minDamage > 0f && stats.maxDamage > 0f) ? $"{stats.minDamage:F0}-{stats.maxDamage:F0}" : stats.damage.ToString("F0");
                if (stats.critChance > 0f)
                {
                    damageText += $" ({stats.critChance * 100f:F0}% Crit)";
                }
                towerStatsText.text = $"Dmg: {damageText}\nRange: {stats.range}\nSpd: {stats.fireRate:F1}/s\n\n" +
                                     $"[Base T{selectedTower.BaseTier}]  [Body T{selectedTower.BodyTier}]  [Wpn T{selectedTower.WeaponTier}]";
            }

            // Update modular part buttons
            UpdatePartButton(baseUpgradeBtn, baseCostTxt, TowerPartType.Base);
            UpdatePartButton(bodyUpgradeBtn, bodyCostTxt, TowerPartType.Body);
            UpdatePartButton(weaponUpgradeBtn, weaponCostTxt, TowerPartType.Weapon);

            // Sell Button Setup
            int totalSpent = selectedTower.Data.tier1.cost;
            if (selectedTower.BaseTier >= 2 && selectedTower.Data.baseTiers.Length >= 2) totalSpent += selectedTower.Data.baseTiers[1].cost;
            if (selectedTower.BaseTier >= 3 && selectedTower.Data.baseTiers.Length >= 3) totalSpent += selectedTower.Data.baseTiers[2].cost;

            if (selectedTower.BodyTier >= 2 && selectedTower.Data.bodyTiers.Length >= 2) totalSpent += selectedTower.Data.bodyTiers[1].cost;
            if (selectedTower.BodyTier >= 3 && selectedTower.Data.bodyTiers.Length >= 3) totalSpent += selectedTower.Data.bodyTiers[2].cost;

            if (selectedTower.WeaponTier >= 2 && selectedTower.Data.weaponTiers.Length >= 2) totalSpent += selectedTower.Data.weaponTiers[1].cost;
            if (selectedTower.WeaponTier >= 3 && selectedTower.Data.weaponTiers.Length >= 3) totalSpent += selectedTower.Data.weaponTiers[2].cost;

            int refund = Mathf.FloorToInt(totalSpent * 0.70f);

            if (sellRefundText != null) sellRefundText.text = $"Sell: +{refund}G";

            // Priority Dropdown Setup
            if (priorityDropdown != null)
            {
                priorityDropdown.SetValueWithoutNotify((int)selectedTower.Priority);
            }
        }

        private void UpdatePartButton(Button btn, TextMeshProUGUI costTxt, TowerPartType partType)
        {
            if (btn == null || costTxt == null || selectedTower == null) return;

            int currentTier = 1;
            Data.TowerPartData[] tiers = null;

            if (partType == TowerPartType.Base)
            {
                currentTier = selectedTower.BaseTier;
                tiers = selectedTower.Data.baseTiers;
            }
            else if (partType == TowerPartType.Body)
            {
                currentTier = selectedTower.BodyTier;
                tiers = selectedTower.Data.bodyTiers;
            }
            else if (partType == TowerPartType.Weapon)
            {
                currentTier = selectedTower.WeaponTier;
                tiers = selectedTower.Data.weaponTiers;
            }

            if (currentTier >= 3)
            {
                btn.interactable = false;
                string partName = "";
                if (partType == TowerPartType.Base) partName = "Base";
                else if (partType == TowerPartType.Body) partName = "Body";
                else if (partType == TowerPartType.Weapon) partName = "Wpn";
                costTxt.text = $"{partName} Lvl 3: MAX";
                var cg = btn.GetComponent<CanvasGroup>();
                if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0.5f;
            }
            else
            {
                int nextTier = currentTier + 1;
                int cost = (tiers != null && nextTier - 1 < tiers.Length) ? RunPerkManager.GetUpgradeCost(tiers[nextTier - 1].cost) : 0;

                string partName = "";
                if (partType == TowerPartType.Base) partName = "Base";
                else if (partType == TowerPartType.Body) partName = "Body";
                else if (partType == TowerPartType.Weapon) partName = "Wpn";

                costTxt.text = $"{partName} Lvl {currentTier}->{nextTier}: {cost}G";

                bool canAfford = GameManager.Instance.CurrentGold >= cost;
                btn.interactable = canAfford;
                var cg = btn.GetComponent<CanvasGroup>();
                if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
                if (cg != null) cg.alpha = canAfford ? 1.0f : 0.5f;
            }
        }

        private void HandleGoldChanged(int currentGold)
        {
            if (selectedTower != null)
            {
                UpdateStatsUI();
            }
        }

        private void OnUpgradeBaseClicked()
        {
            if (selectedTower != null && TowerUpgrade.Instance != null)
            {
                if (TowerUpgrade.Instance.UpgradePart(selectedTower, TowerPartType.Base))
                {
                    UpdateStatsUI();
                }
            }
        }

        private void OnUpgradeBodyClicked()
        {
            if (selectedTower != null && TowerUpgrade.Instance != null)
            {
                if (TowerUpgrade.Instance.UpgradePart(selectedTower, TowerPartType.Body))
                {
                    UpdateStatsUI();
                }
            }
        }

        private void OnUpgradeWeaponClicked()
        {
            if (selectedTower != null && TowerUpgrade.Instance != null)
            {
                if (TowerUpgrade.Instance.UpgradePart(selectedTower, TowerPartType.Weapon))
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
