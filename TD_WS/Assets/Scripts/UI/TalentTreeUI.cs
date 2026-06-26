using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerDefense.Core;
using TowerDefense.Utils;

namespace TowerDefense.UI
{
    public class TalentTreeUI : MonoBehaviour
    {
        public static TalentTreeUI Instance { get; private set; }

        [Header("UI References")]
        public TextMeshProUGUI tokensText;
        public Button resetButton;
        public Button closeButton;
        public RectTransform scrollContent;
        public Transform linesContainer;
        public Transform buttonsContainer;

        [Header("Prefabs/Prototypes (Optional)")]
        // If null, we will create them dynamically!
        public GameObject nodeButtonPrefab;

        private List<Button> nodeButtons = new List<Button>();
        private List<UnityEngine.UI.Image> activeLines = new List<UnityEngine.UI.Image>();
        private TMP_FontAsset projectFont;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Set up button listeners
            if (resetButton != null) resetButton.onClick.AddListener(OnResetClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);

            // Fetch the default font from an existing text element in the scene
            if (UIManager.Instance != null && UIManager.Instance.hpText != null)
            {
                projectFont = UIManager.Instance.hpText.font;
            }

            BuildTreeUI();
        }

        private void OnEnable()
        {
            RefreshUI();
            EventBus.OnPlayerProgressChanged += RefreshUI;
        }

        private void OnDisable()
        {
            EventBus.OnPlayerProgressChanged -= RefreshUI;
        }

        public void BuildTreeUI()
        {
            // Clear existing elements
            foreach (Transform child in buttonsContainer) Destroy(child.gameObject);
            foreach (Transform child in linesContainer) Destroy(child.gameObject);
            nodeButtons.Clear();
            activeLines.Clear();

            if (TalentTreeManager.Instance == null)
            {
                Debug.LogError("[Talents] TalentTreeManager.Instance is null! Cannot build Tree UI.");
                return;
            }

            // Create buttons for each node
            foreach (var node in TalentTreeManager.Instance.Nodes)
            {
                GameObject btnObj = new GameObject($"Node_{node.id}", typeof(RectTransform));
                btnObj.transform.SetParent(buttonsContainer, false);

                RectTransform rt = btnObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = node.position;
                rt.sizeDelta = new Vector2(90f, 65f); // Nice node size

                // Background Image
                UnityEngine.UI.Image bgImg = btnObj.AddComponent<UnityEngine.UI.Image>();
                // Create a clean background: sprite from standard buttons if possible, or just solid color
                bgImg.type = UnityEngine.UI.Image.Type.Sliced;

                // Button component
                Button btn = btnObj.AddComponent<Button>();
                int nodeId = node.id;
                btn.onClick.AddListener(() => OnNodeClicked(nodeId));

                // Add TextMeshProUGUI child
                GameObject textObj = new GameObject("Text", typeof(RectTransform));
                textObj.transform.SetParent(btnObj.transform, false);
                RectTransform textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;

                TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
                if (projectFont != null) txt.font = projectFont;
                txt.fontSize = 11f;
                txt.alignment = TextAlignmentOptions.Center;
                txt.lineSpacing = -10f;
                txt.color = Color.white;

                nodeButtons.Add(btn);
            }

            DrawConnections();
            RefreshNodeStates();
        }

        private void DrawConnections()
        {
            // Clear old lines
            foreach (var line in activeLines)
            {
                if (line != null) Destroy(line.gameObject);
            }
            activeLines.Clear();

            if (TalentTreeManager.Instance == null) return;

            HashSet<string> drawn = new HashSet<string>();
            foreach (var node in TalentTreeManager.Instance.Nodes)
            {
                foreach (var connId in node.connections)
                {
                    var otherNode = TalentTreeManager.Instance.Nodes.Find(n => n.id == connId);
                    if (otherNode == null) continue;

                    string key = node.id < otherNode.id ? $"{node.id}_{otherNode.id}" : $"{otherNode.id}_{node.id}";
                    if (drawn.Contains(key)) continue;
                    drawn.Add(key);

                    // Create line image
                    GameObject lineObj = new GameObject("Line_" + key, typeof(RectTransform));
                    lineObj.transform.SetParent(linesContainer, false);
                    UnityEngine.UI.Image img = lineObj.AddComponent<UnityEngine.UI.Image>();

                    RectTransform rt = lineObj.GetComponent<RectTransform>();
                    Vector2 p1 = node.position;
                    Vector2 p2 = otherNode.position;

                    Vector2 dir = p2 - p1;
                    float distance = dir.magnitude;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                    rt.sizeDelta = new Vector2(distance, 4f); // Width is distance, Height is thickness
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0f, 0.5f); // Pivot at start of line
                    rt.anchoredPosition = p1;
                    rt.localRotation = Quaternion.Euler(0f, 0f, angle);

                    activeLines.Add(img);
                }
            }
        }

        public void RefreshUI()
        {
            if (PlayerProgressManager.Instance != null && tokensText != null)
            {
                tokensText.text = $"Available Tokens: {PlayerProgressManager.Instance.TechTokens}";
            }

            RefreshNodeStates();
            RefreshLineColors();
        }

        private void RefreshNodeStates()
        {
            if (TalentTreeManager.Instance == null) return;

            for (int i = 0; i < nodeButtons.Count; i++)
            {
                var btn = nodeButtons[i];
                if (btn == null) continue;

                var node = TalentTreeManager.Instance.Nodes[i];
                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                var img = btn.GetComponent<UnityEngine.UI.Image>();

                bool unlocked = TalentTreeManager.Instance.IsNodeUnlocked(node.id);
                bool maxed = node.level >= node.maxLevel;

                // Update text
                if (txt != null)
                {
                    if (!unlocked)
                    {
                        txt.text = $"{node.name}\n<color=#999999>LOCKED</color>";
                    }
                    else if (maxed)
                    {
                        txt.text = $"{node.name}\n<color=#FFD700>Lvl {node.level}/{node.maxLevel}\nMAX</color>";
                    }
                    else
                    {
                        int cost = TalentTreeManager.Instance.GetUpgradeCost(node.id);
                        txt.text = $"{node.name}\n<color=#00FF00>Lvl {node.level}/{node.maxLevel}\nCost: {cost}</color>";
                    }
                }

                // Update button visuals/colors
                if (img != null)
                {
                    if (!unlocked)
                    {
                        img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Locked - Dark Grey
                        btn.interactable = false;
                    }
                    else if (node.level > 0)
                    {
                        if (maxed)
                        {
                            img.color = new Color(0.8f, 0.65f, 0.0f, 1.0f); // Max level - Shiny Gold
                        }
                        else
                        {
                            img.color = new Color(0.1f, 0.6f, 0.1f, 1.0f); // Unlocked & active - Green
                        }
                        btn.interactable = true;
                    }
                    else
                    {
                        img.color = new Color(0.5f, 0.35f, 0.1f, 1.0f); // Unlocked but 0 level - Bronze / Orange
                        btn.interactable = true;
                    }
                }
            }
        }

        private void RefreshLineColors()
        {
            if (TalentTreeManager.Instance == null || activeLines.Count == 0) return;

            int lineIndex = 0;
            HashSet<string> drawn = new HashSet<string>();

            foreach (var node in TalentTreeManager.Instance.Nodes)
            {
                foreach (var connId in node.connections)
                    {
                        var otherNode = TalentTreeManager.Instance.Nodes.Find(n => n.id == connId);
                        if (otherNode == null) continue;

                        string key = node.id < otherNode.id ? $"{node.id}_{otherNode.id}" : $"{otherNode.id}_{node.id}";
                        if (drawn.Contains(key)) continue;
                        drawn.Add(key);

                        if (lineIndex < activeLines.Count && activeLines[lineIndex] != null)
                        {
                            var img = activeLines[lineIndex];
                            if (node.level > 0 && otherNode.level > 0)
                            {
                                img.color = new Color(1.0f, 0.85f, 0.0f, 1.0f); // Golden
                            }
                            else if (node.level > 0 || otherNode.level > 0)
                            {
                                img.color = new Color(0.9f, 0.6f, 0.1f, 0.7f); // Orange-yellow
                            }
                            else
                            {
                                img.color = new Color(0.25f, 0.25f, 0.25f, 0.5f); // Dark Grey
                            }
                        }
                        lineIndex++;
                    }
            }
        }

        private void OnNodeClicked(int nodeId)
        {
            if (TalentTreeManager.Instance != null)
            {
                if (TalentTreeManager.Instance.TryUpgradeNode(nodeId))
                {
                    RefreshUI();
                }
            }
        }

        private void OnResetClicked()
        {
            if (TalentTreeManager.Instance != null)
            {
                TalentTreeManager.Instance.ResetTree();
                RefreshUI();
            }
        }

        private void OnCloseClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CloseTalentTree();
            }
        }
    }
}