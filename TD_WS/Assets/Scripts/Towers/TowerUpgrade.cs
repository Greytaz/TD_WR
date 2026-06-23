using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Utils;
using TowerDefense.Data;

namespace TowerDefense.Towers
{
    public enum TowerPartType
    {
        Base,
        Body,
        Weapon
    }

    public class TowerUpgrade : MonoBehaviour
    {
        public static TowerUpgrade Instance { get; private set; }

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

        public bool UpgradePart(TowerBase tower, TowerPartType partType)
        {
            if (tower == null) return false;

            int currentTier = 1;
            int nextTier = 1;
            int cost = 0;

            if (partType == TowerPartType.Base)
            {
                currentTier = tower.BaseTier;
                if (currentTier >= 3) return false;
                nextTier = currentTier + 1;
                cost = tower.Data.baseTiers[nextTier - 1].cost;
            }
            else if (partType == TowerPartType.Body)
            {
                currentTier = tower.BodyTier;
                if (currentTier >= 3) return false;
                nextTier = currentTier + 1;
                cost = tower.Data.bodyTiers[nextTier - 1].cost;
            }
            else if (partType == TowerPartType.Weapon)
            {
                currentTier = tower.WeaponTier;
                if (currentTier >= 3) return false;
                nextTier = currentTier + 1;
                cost = tower.Data.weaponTiers[nextTier - 1].cost;
            }

            if (GameManager.Instance.SpendGold(cost))
            {
                if (partType == TowerPartType.Base)
                {
                    tower.SetBaseTier(nextTier);
                }
                else if (partType == TowerPartType.Body)
                {
                    tower.SetBodyTier(nextTier);
                }
                else if (partType == TowerPartType.Weapon)
                {
                    tower.SetWeaponTier(nextTier);
                }
                
                // Spawn upgrade particle burst
                if (Effects.ParticleManager.Instance != null)
                {
                    Effects.ParticleManager.Instance.SpawnParticle("UpgradeBurst", tower.transform.position, 1.5f);
                }

                // Update placement indicators if open
                TowerPlacement.Instance.UpdateSelectedTowerIndicator();
                return true;
            }

            Debug.Log("Not enough gold to upgrade part!");
            return false;
        }

        public bool UpgradeTower(TowerBase tower)
        {
            // Legacy fallback, upgrades weapon/head first or can just be deprecated
            return UpgradePart(tower, TowerPartType.Weapon);
        }

        public void SellTower(TowerBase tower)
        {
            if (tower == null) return;

            // Compute total gold spent across all purchased modular parts
            int totalSpent = tower.Data.tier1.cost;

            // Base upgrades
            if (tower.BaseTier >= 2 && tower.Data.baseTiers.Length >= 2) totalSpent += tower.Data.baseTiers[1].cost;
            if (tower.BaseTier >= 3 && tower.Data.baseTiers.Length >= 3) totalSpent += tower.Data.baseTiers[2].cost;

            // Body upgrades
            if (tower.BodyTier >= 2 && tower.Data.bodyTiers.Length >= 2) totalSpent += tower.Data.bodyTiers[1].cost;
            if (tower.BodyTier >= 3 && tower.Data.bodyTiers.Length >= 3) totalSpent += tower.Data.bodyTiers[2].cost;

            // Weapon upgrades
            if (tower.WeaponTier >= 2 && tower.Data.weaponTiers.Length >= 2) totalSpent += tower.Data.weaponTiers[1].cost;
            if (tower.WeaponTier >= 3 && tower.Data.weaponTiers.Length >= 3) totalSpent += tower.Data.weaponTiers[2].cost;

            // Refund 70%
            int refund = Mathf.FloorToInt(totalSpent * 0.70f);
            GameManager.Instance.AddGold(refund);

            // Free Grid Cell
            if (GridManager.Instance.WorldToGrid(tower.transform.position, out int x, out int z))
            {
                GridManager.Instance.FreeCell(x, z);
            }

            // Spawn sell VFX
            if (Effects.ParticleManager.Instance != null)
            {
                Effects.ParticleManager.Instance.SpawnParticle("SellDust", tower.transform.position, 1.0f);
            }

            // Deselect and Destroy tower
            TowerPlacement.Instance.DeselectTower();
            Destroy(tower.gameObject);
        }
    }
}
