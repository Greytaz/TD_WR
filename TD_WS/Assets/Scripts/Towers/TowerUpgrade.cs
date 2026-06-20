using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Utils;

namespace TowerDefense.Towers
{
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

        public bool UpgradeTower(TowerBase tower)
        {
            if (tower == null || tower.CurrentTier >= 3) return false;

            int nextTier = tower.CurrentTier + 1;
            Data.TowerTierData nextStats = tower.Data.GetTierData(nextTier);

            if (nextStats == null) return false;

            int cost = nextStats.cost;

            if (GameManager.Instance.SpendGold(cost))
            {
                tower.SetTier(nextTier);
                
                // Spawn upgrade particle burst
                if (Effects.ParticleManager.Instance != null)
                {
                    Effects.ParticleManager.Instance.SpawnParticle("UpgradeBurst", tower.transform.position, 1.5f);
                }

                // Update placement indicators if open
                TowerPlacement.Instance.UpdateSelectedTowerIndicator();
                return true;
            }

            Debug.Log("Not enough gold to upgrade!");
            return false;
        }

        public void SellTower(TowerBase tower)
        {
            if (tower == null) return;

            // Compute total gold spent
            int totalSpent = tower.Data.tier1.cost;
            if (tower.CurrentTier >= 2)
            {
                totalSpent += tower.Data.tier2.cost;
            }
            if (tower.CurrentTier >= 3)
            {
                totalSpent += tower.Data.tier3.cost;
            }

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
