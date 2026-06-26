using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Utils;

namespace TowerDefense.Core
{
    [System.Serializable]
    public class TalentNode
    {
        public int id;
        public string name;
        public int level;
        public int maxLevel;
        public List<int> connections = new List<int>();
        public Vector2 position; // Relative coordinates for layout in the UI
    }

    public class TalentTreeManager : MonoBehaviour
    {
        public static TalentTreeManager Instance { get; private set; }

        private const string KEY_NODE_PREFIX = "TalentNode_";

        [SerializeField] private List<TalentNode> nodes = new List<TalentNode>();
        public List<TalentNode> Nodes => nodes;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                InitializeTree();
                LoadTreeState();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeTree()
        {
            nodes.Clear();

            // Setup 20 talent nodes with names, positions, and connections.
            // Node 0 is the center starting node.
            
            // Positions on concentric circles:
            // Center: Node 0 (0, 0)
            // Circle 1 (Radius 120): Nodes 1-5 (angles 0, 72, 144, 216, 288 degrees)
            // Circle 2 (Radius 240): Nodes 6-15 (angles around)
            // Circle 3 (Radius 340): Nodes 16-19

            // Node 0: Center (Start)
            nodes.Add(new TalentNode { id = 0, name = "Core Nexus", maxLevel = 5, position = new Vector2(0f, 0f) });

            // Circle 1 (Radius 120)
            nodes.Add(new TalentNode { id = 1, name = "Apex Power", maxLevel = 5, position = new Vector2(0f, 120f) });
            nodes.Add(new TalentNode { id = 2, name = "East Focus", maxLevel = 5, position = new Vector2(114f, 37f) });
            nodes.Add(new TalentNode { id = 3, name = "Southeastern Might", maxLevel = 5, position = new Vector2(71f, -97f) });
            nodes.Add(new TalentNode { id = 4, name = "Southwestern Might", maxLevel = 5, position = new Vector2(-71f, -97f) });
            nodes.Add(new TalentNode { id = 5, name = "West Focus", maxLevel = 5, position = new Vector2(-114f, 37f) });

            // Circle 2 (Radius 240)
            nodes.Add(new TalentNode { id = 6, name = "Northern Edge Left", maxLevel = 5, position = new Vector2(-60f, 230f) });
            nodes.Add(new TalentNode { id = 7, name = "Northern Edge Right", maxLevel = 5, position = new Vector2(60f, 230f) });
            nodes.Add(new TalentNode { id = 8, name = "Northeastern Edge", maxLevel = 5, position = new Vector2(194f, 141f) });
            nodes.Add(new TalentNode { id = 9, name = "Eastern Gate", maxLevel = 5, position = new Vector2(230f, 20f) });
            nodes.Add(new TalentNode { id = 10, name = "Southeastern Gate", maxLevel = 5, position = new Vector2(160f, -170f) });
            nodes.Add(new TalentNode { id = 11, name = "Southern Border Right", maxLevel = 5, position = new Vector2(50f, -235f) });
            nodes.Add(new TalentNode { id = 12, name = "Southern Border Left", maxLevel = 5, position = new Vector2(-50f, -235f) });
            nodes.Add(new TalentNode { id = 13, name = "Southwestern Gate", maxLevel = 5, position = new Vector2(-160f, -170f) });
            nodes.Add(new TalentNode { id = 14, name = "Western Gate", maxLevel = 5, position = new Vector2(-230f, 20f) });
            nodes.Add(new TalentNode { id = 15, name = "Northwestern Edge", maxLevel = 5, position = new Vector2(-194f, 141f) });

            // Circle 3 (Radius 340)
            nodes.Add(new TalentNode { id = 16, name = "Zenith Apex", maxLevel = 5, position = new Vector2(0f, 340f) });
            nodes.Add(new TalentNode { id = 17, name = "Orient Sun", maxLevel = 5, position = new Vector2(340f, 0f) });
            nodes.Add(new TalentNode { id = 18, name = "Nadir Anchor", maxLevel = 5, position = new Vector2(0f, -340f) });
            nodes.Add(new TalentNode { id = 19, name = "Occident Veil", maxLevel = 5, position = new Vector2(-340f, 0f) });

            // Setup connections (undirected edges)
            // Center to Circle 1
            AddConnection(0, 1);
            AddConnection(0, 2);
            AddConnection(0, 3);
            AddConnection(0, 4);
            AddConnection(0, 5);

            // Circle 1 internal ring connections
            AddConnection(1, 2);
            AddConnection(2, 3);
            AddConnection(3, 4);
            AddConnection(4, 5);
            AddConnection(5, 1);

            // Circle 1 to Circle 2 branches
            AddConnection(1, 6);
            AddConnection(1, 7);
            AddConnection(2, 8);
            AddConnection(2, 9);
            AddConnection(3, 10);
            AddConnection(3, 11);
            AddConnection(4, 12);
            AddConnection(4, 13);
            AddConnection(5, 14);
            AddConnection(5, 15);

            // Circle 2 to Circle 3 extremities
            AddConnection(6, 16);
            AddConnection(7, 16);
            AddConnection(8, 17);
            AddConnection(9, 17);
            AddConnection(10, 18);
            AddConnection(11, 18);
            AddConnection(12, 18);
            AddConnection(13, 19);
            AddConnection(14, 19);
            AddConnection(15, 19);
        }

        private void AddConnection(int id1, int id2)
        {
            TalentNode n1 = nodes.Find(n => n.id == id1);
            TalentNode n2 = nodes.Find(n => n.id == id2);
            if (n1 != null && n2 != null)
            {
                if (!n1.connections.Contains(id2)) n1.connections.Add(id2);
                if (!n2.connections.Contains(id1)) n2.connections.Add(id1);
            }
        }

        public void LoadTreeState()
        {
            foreach (var node in nodes)
            {
                node.level = PlayerPrefs.GetInt(KEY_NODE_PREFIX + node.id, 0);
            }
            Debug.Log("[Talents] Talent tree levels loaded successfully.");
        }

        public void SaveTreeState()
        {
            foreach (var node in nodes)
            {
                PlayerPrefs.SetInt(KEY_NODE_PREFIX + node.id, node.level);
            }
            PlayerPrefs.Save();
            Debug.Log("[Talents] Talent tree levels saved successfully.");
        }

        public void ResetTree()
        {
            int totalRefund = 0;
            foreach (var node in nodes)
            {
                // Calculate refunded tokens: sum of (1 + 2 + ... + current_level)
                for (int lvl = 1; lvl <= node.level; lvl++)
                {
                    totalRefund += lvl;
                }
                node.level = 0;
            }

            if (totalRefund > 0 && PlayerProgressManager.Instance != null)
            {
                PlayerProgressManager.Instance.RefundTechTokens(totalRefund);
            }

            SaveTreeState();
            RecalculateTowerStats();
            Debug.Log($"[Talents] Talent tree reset. Refunded {totalRefund} tokens.");
        }

        public float GetTotalDamageMultiplier()
        {
            int totalLevels = 0;
            foreach (var node in nodes)
            {
                totalLevels += node.level;
            }
            // Each node level grants +1% damage
            return 1.0f + 0.01f * totalLevels;
        }

        public bool IsNodeUnlocked(int id)
        {
            // Core node (0) is always unlocked for purchase
            if (id == 0) return true;

            TalentNode target = nodes.Find(n => n.id == id);
            if (target == null) return false;

            // Check if any of its connections is unlocked (level > 0)
            foreach (var connId in target.connections)
            {
                TalentNode connNode = nodes.Find(n => n.id == connId);
                if (connNode != null && connNode.level > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetUpgradeCost(int id)
        {
            TalentNode target = nodes.Find(n => n.id == id);
            if (target == null) return 0;

            // Upgrade level L to L+1 costs L+1 tokens. E.g. 0->1 costs 1, 1->2 costs 2, etc.
            return target.level + 1;
        }

        public bool TryUpgradeNode(int id)
        {
            if (!IsNodeUnlocked(id))
            {
                Debug.LogWarning($"[Talents] Node {id} is locked and cannot be purchased.");
                return false;
            }

            TalentNode target = nodes.Find(n => n.id == id);
            if (target == null || target.level >= target.maxLevel)
            {
                Debug.LogWarning($"[Talents] Node {id} cannot be upgraded further (Max Level reached).");
                return false;
            }

            int cost = GetUpgradeCost(id);
            if (PlayerProgressManager.Instance != null && PlayerProgressManager.Instance.SpendTechTokens(cost))
            {
                target.level++;
                SaveTreeState();
                RecalculateTowerStats();
                Debug.Log($"[Talents] Node {id} successfully upgraded to level {target.level}.");
                return true;
            }

            Debug.LogWarning("[Talents] Not enough Tech Tokens to buy upgrade.");
            return false;
        }

        private void RecalculateTowerStats()
        {
            // Re-compile stats for all active towers
            var activeTowers = Object.FindObjectsByType<Towers.TowerBase>(FindObjectsSortMode.None);
            foreach (var tower in activeTowers)
            {
                tower.UpdateStats();
            }
        }
    }
}