using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Utils;
using TowerDefense.Data;
using TowerDefense.UI;

namespace TowerDefense.Core
{
    [System.Serializable]
    public class PerkData
    {
        public string id;
        public string title;
        public string description;
        public string type;
        public float value;
        public string value_type; // "percent" or "flat"
    }

    [System.Serializable]
    public class PerkGroup
    {
        public int waveCompleted;
        public int pickCount;
        public List<PerkData> options;
    }

    public class RunPerkManager : MonoBehaviour
    {
        public static RunPerkManager Instance { get; private set; }

        private List<string> activeRunPerks = new List<string>();
        public List<string> ActiveRunPerks => activeRunPerks;

        private List<PerkGroup> perkGroups = new List<PerkGroup>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializePerkGroups();
            }
            else if (Instance != this)
            {
                Destroy(this);
            }
        }

        private void Start()
        {
            // Ensure PerkChoiceUI is instantiated
            if (PerkChoiceUI.Instance == null)
            {
                GameObject perkUIObj = new GameObject("PerkChoiceUI", typeof(PerkChoiceUI));
            }
        }

        private void OnEnable()
        {
            EventBus.OnWaveCompleted += HandleWaveCompleted;
        }

        private void OnDisable()
        {
            EventBus.OnWaveCompleted -= HandleWaveCompleted;
        }

        private void HandleWaveCompleted(int waveIndex)
        {
            // Wave Bonus Gold perk logic
            if (IsPerkActive("wave_bonus") && waveIndex % 5 == 0)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddGold(25);
                    Debug.Log("[Perks] Wave Bonus Gold applied: +25 Gold.");
                }
            }

            // Tactical Economy gold bonus logic (+10% of wave baseline clear gold)
            if (IsPerkActive("tactical_economy"))
            {
                int baseBonus = 50 + (waveIndex * 10);
                int extraGold = Mathf.FloorToInt(baseBonus * 0.10f);
                if (GameManager.Instance != null && extraGold > 0)
                {
                    GameManager.Instance.AddGold(extraGold);
                    Debug.Log($"[Perks] Tactical Economy: +{extraGold} Gold (10% wave completed bonus).");
                }
            }

            CheckPerkChoiceAfterWave(waveIndex);
        }

        public bool IsPerkActive(string perkId)
        {
            return activeRunPerks.Contains(perkId);
        }

        public void CheckPerkChoiceAfterWave(int completedWave)
        {
            if (completedWave % 5 == 0)
            {
                ShowPerkChoice(completedWave);
            }
        }

        public void ShowPerkChoice(int completedWave)
        {
            Debug.Log($"[Perks] Showing perk choice after wave {completedWave}.");
            
            // Pause the game
            Time.timeScale = 0f;

            // Get options
            List<PerkData> options = GetPerkOptionsForWave(completedWave);
            if (options != null && options.Count >= 2)
            {
                if (PerkChoiceUI.Instance != null)
                {
                    PerkChoiceUI.Instance.Show(options);
                }
                else
                {
                    Debug.LogError("[Perks] PerkChoiceUI.Instance is null!");
                    // Safety fallback: auto-choose first option and unpause
                    ChoosePerk(options[0].id);
                }
            }
            else
            {
                Debug.LogWarning($"[Perks] No pre-defined perk group found for wave {completedWave}, skipping choice.");
                // Unpause the game
                if (GameManager.Instance != null)
                {
                    Time.timeScale = GameManager.Instance.TargetTimeScale;
                }
                else
                {
                    Time.timeScale = 1f;
                }
            }
        }

        public void ChoosePerk(string perkId)
        {
            ApplyPerk(perkId);

            // Close UI
            if (PerkChoiceUI.Instance != null)
            {
                PerkChoiceUI.Instance.Hide();
            }

            // Unpause the game and restore speed scale
            if (GameManager.Instance != null)
            {
                Time.timeScale = GameManager.Instance.TargetTimeScale;
            }
            else
            {
                Time.timeScale = 1f;
            }
        }

        public void ApplyPerk(string perkId)
        {
            if (!activeRunPerks.Contains(perkId))
            {
                activeRunPerks.Add(perkId);
            }

            // Handle instant gold perk
            if (perkId == "emergency_funds")
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddGold(50);
                }
            }

            // Recalculate stats of all built towers
            var activeTowers = Object.FindObjectsByType<Towers.TowerBase>(FindObjectsSortMode.None);
            foreach (var tower in activeTowers)
            {
                tower.UpdateStats();
            }

            // Refresh UI panels if opened
            if (UpgradePanelControllerInstance() != null)
            {
                // Trigger a refresh on UpgradePanel
                var up = UpgradePanelControllerInstance().GetComponent<UpgradePanel>();
                if (up != null && up.gameObject.activeInHierarchy)
                {
                    // Invoking UpdateStatsUI using Reflection isn't needed if we can just trigger it or gold event triggers it.
                    // But we can let it redraw if needed. Gold triggers it.
                }
            }

            Debug.Log($"[Perks] Applied run perk: {perkId}");
        }

        private GameObject UpgradePanelControllerInstance()
        {
            return GameObject.Find("UpgradeSidePanel");
        }

        public void LoadActiveRunPerks(List<string> perks)
        {
            activeRunPerks.Clear();
            if (perks != null)
            {
                activeRunPerks.AddRange(perks);
            }
            Debug.Log($"[Perks] Restored {activeRunPerks.Count} active run perks.");
        }

        public void ResetRunPerks()
        {
            activeRunPerks.Clear();
            Debug.Log("[Perks] Reset all current run perks.");
        }

        public List<PerkData> GetPerkOptionsForWave(int wave)
        {
            PerkGroup group = perkGroups.Find(g => g.waveCompleted == wave);
            if (group != null)
            {
                return group.options;
            }
            return null;
        }

        public static int GetBuildCost(TowerData data)
        {
            if (data == null) return 0;
            int baseCost = data.tier1.cost;
            if (Instance != null && Instance.IsPerkActive("efficient_construction"))
            {
                baseCost = Mathf.FloorToInt(baseCost * 0.90f);
            }
            return baseCost;
        }

        public static int GetUpgradeCost(int baseCost)
        {
            if (Instance != null && Instance.IsPerkActive("upgrade_discount"))
            {
                return Mathf.FloorToInt(baseCost * 0.90f);
            }
            return baseCost;
        }

        public void ApplyPerkStats(TowerType type, TowerTierData stats)
        {
            if (stats == null) return;

            // smart_targeting (TowerRange: +5% range of all towers)
            if (IsPerkActive("smart_targeting"))
            {
                stats.range *= 1.05f;
            }

            // faster_reload (AttackSpeed: +7% fire rate of all towers)
            if (IsPerkActive("faster_reload"))
            {
                stats.fireRate *= 1.07f;
            }

            // master_engineer (AllTowerStats: +5% damage, +5% range, +5% fire rate)
            if (IsPerkActive("master_engineer"))
            {
                stats.damage *= 1.05f;
                stats.minDamage *= 1.05f;
                stats.maxDamage *= 1.05f;
                stats.range *= 1.05f;
                stats.fireRate *= 1.05f;
            }

            if (type == TowerType.Archer)
            {
                // reinforced_barrels (ShooterDamage: +10% damage)
                if (IsPerkActive("reinforced_barrels"))
                {
                    stats.damage *= 1.10f;
                    stats.minDamage *= 1.10f;
                    stats.maxDamage *= 1.10f;
                }

                // precision_protocol (ShooterCritChance: +10% crit chance)
                if (IsPerkActive("precision_protocol"))
                {
                    stats.critChance += 0.10f;
                }
            }
            else if (type == TowerType.Mage)
            {
                // charged_coils (MageStunDuration: +10% stun duration)
                if (IsPerkActive("charged_coils"))
                {
                    stats.stunDuration *= 1.10f;
                }

                // shock_amplifier (MageMiniStunChance: +15% stun chance and ensures stunDuration > 0)
                if (IsPerkActive("shock_amplifier"))
                {
                    stats.stunChance += 0.15f;
                    if (stats.stunDuration <= 0f)
                    {
                        stats.stunDuration = 0.25f; // micro-stun
                    }
                }

                // overcharged_mages (MageAttackSpeed: +10% Mage fire rate)
                if (IsPerkActive("overcharged_mages"))
                {
                    stats.fireRate *= 1.10f;
                }
            }
            else if (type == TowerType.Cannon)
            {
                // heavy_shells (CannonAOEDamage: +10% Cannon damage)
                if (IsPerkActive("heavy_shells"))
                {
                    stats.damage *= 1.10f;
                    stats.minDamage *= 1.10f;
                    stats.maxDamage *= 1.10f;
                }

                // explosive_radius (CannonExplosionRadius: +15% splash radius)
                if (IsPerkActive("explosive_radius"))
                {
                    stats.splashRadius *= 1.15f;
                }
            }
        }

        private void InitializePerkGroups()
        {
            // Wave 5
            perkGroups.Add(new PerkGroup
            {
                waveCompleted = 5,
                pickCount = 1,
                options = new List<PerkData>
                {
                    new PerkData { id = "reinforced_barrels", title = "Reinforced Barrels", description = "+10% Archer Damage", type = "ShooterDamage", value = 10, value_type = "percent" },
                    new PerkData { id = "charged_coils", title = "Charged Coils", description = "+10% Mage Stun Duration", type = "MageStunDuration", value = 10, value_type = "percent" }
                }
            });

            // Wave 10
            perkGroups.Add(new PerkGroup
            {
                waveCompleted = 10,
                pickCount = 1,
                options = new List<PerkData>
                {
                    new PerkData { id = "heavy_shells", title = "Heavy Shells", description = "+10% Cannon AOE Damage", type = "CannonAOEDamage", value = 10, value_type = "percent" },
                    new PerkData { id = "smart_targeting", title = "Smart Targeting", description = "+5% Range of all Towers", type = "TowerRange", value = 5, value_type = "percent" }
                }
            });

            // Wave 15
            perkGroups.Add(new PerkGroup
            {
                waveCompleted = 15,
                pickCount = 1,
                options = new List<PerkData>
                {
                    new PerkData { id = "combat_salvage", title = "Combat Salvage", description = "+1 Extra Token per Boss killed in current run", type = "ExtraBossToken", value = 1, value_type = "flat" },
                    new PerkData { id = "emergency_funds", title = "Emergency Funds", description = "+50 Gold immediately", type = "InstantGold", value = 50, value_type = "flat" }
                }
            });

            // Wave 20
            perkGroups.Add(new PerkGroup
            {
                waveCompleted = 20,
                pickCount = 1,
                options = new List<PerkData>
                {
                    new PerkData { id = "faster_reload", title = "Faster Reload", description = "+7% Attack Speed for all Towers", type = "AttackSpeed", value = 7, value_type = "percent" },
                    new PerkData { id = "shock_amplifier", title = "Shock Amplifier", description = "+15% micro-stun chance for Mage Tower", type = "MageMiniStunChance", value = 15, value_type = "percent" }
                }
            });

            // Wave 25
            perkGroups.Add(new PerkGroup
            {
                waveCompleted = 25,
                pickCount = 1,
                options = new List<PerkData>
                {
                    new PerkData { id = "explosive_radius", title = "Explosive Radius", description = "+15% Cannon Explosion Radius", type = "CannonExplosionRadius", value = 15, value_type = "percent" },
                    new PerkData { id = "precision_protocol", title = "Precision Protocol", description = "+10% Archer Crit Chance", type = "ShooterCritChance", value = 10, value_type = "percent" }
                }
            });

            // Wave 30
            perkGroups.Add(new PerkGroup
            {
                waveCompleted = 30,
                pickCount = 1,
                options = new List<PerkData>
                {
                    new PerkData { id = "wave_bonus", title = "Wave Bonus", description = "+25 Gold after each 5th Wave", type = "WaveBonusGold", value = 25, value_type = "flat" },
                    new PerkData { id = "boss_hunter", title = "Boss Hunter", description = "+15% Damage against Bosses", type = "BossDamage", value = 15, value_type = "percent" }
                }
            });

            // Wave 35
            perkGroups.Add(new PerkGroup
            {
                waveCompleted = 35,
                pickCount = 1,
                options = new List<PerkData>
                {
                    new PerkData { id = "efficient_construction", title = "Efficient Construction", description = "-10% Tower Build Cost", type = "TowerBuildDiscount", value = 10, value_type = "percent" },
                    new PerkData { id = "upgrade_discount", title = "Upgrade Discount", description = "-10% Tower Upgrade Cost", type = "TowerUpgradeDiscount", value = 10, value_type = "percent" }
                }
            });

            // Wave 40
            perkGroups.Add(new PerkGroup
            {
                waveCompleted = 40,
                pickCount = 1,
                options = new List<PerkData>
                {
                    new PerkData { id = "overcharged_mages", title = "Overcharged Mages", description = "+10% Mage Attack Speed", type = "MageAttackSpeed", value = 10, value_type = "percent" },
                    new PerkData { id = "armor_piercing_rounds", title = "Armor Piercing Rounds", description = "Archer ignores 15% Armor (saved parameter)", type = "ShooterArmorPierce", value = 15, value_type = "percent" }
                }
            });

            // Wave 45
            perkGroups.Add(new PerkGroup
            {
                waveCompleted = 45,
                pickCount = 1,
                options = new List<PerkData>
                {
                    new PerkData { id = "siege_engineering", title = "Siege Engineering", description = "+20% Cannon Damage to Slow and Boss enemies", type = "CannonDamageToSlowEnemies", value = 20, value_type = "percent" },
                    new PerkData { id = "tactical_economy", title = "Tactical Economy", description = "+10% wave completed Gold bonus", type = "EndWaveGoldBonus", value = 10, value_type = "percent" }
                }
            });

            // Wave 50
            perkGroups.Add(new PerkGroup
            {
                waveCompleted = 50,
                pickCount = 1,
                options = new List<PerkData>
                {
                    new PerkData { id = "master_engineer", title = "Master Engineer", description = "+5% damage, range and fire rate of all Towers", type = "AllTowerStats", value = 5, value_type = "percent" },
                    new PerkData { id = "token_investor", title = "Token Investor", description = "Double Boss Token rewards", type = "DoubleBossTokens", value = 2, value_type = "flat" }
                }
            });
        }
    }
}