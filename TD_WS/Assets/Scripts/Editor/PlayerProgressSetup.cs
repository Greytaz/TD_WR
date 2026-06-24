#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TowerDefense.Core;
using TowerDefense.UI;

namespace TowerDefense.Editor
{
    public static class PlayerProgressSetup
    {
        [MenuItem("Tower Defense/Setup Player Progress")]
        public static void Setup()
        {
            // Make sure the active scene is SampleScene or load it
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "SampleScene")
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
                }
            }

            // 1. Add PlayerProgressManager to [GameManager]
            GameObject gmObj = GameObject.Find("GameManager");
            if (gmObj != null)
            {
                PlayerProgressManager ppm = gmObj.GetComponent<PlayerProgressManager>();
                if (ppm == null)
                {
                    ppm = gmObj.AddComponent<PlayerProgressManager>();
                    Undo.RegisterCreatedObjectUndo(ppm, "Add PlayerProgressManager");
                    EditorUtility.SetDirty(gmObj);
                    Debug.Log("[Setup] Added PlayerProgressManager component to GameManager GameObject.");
                }
                else
                {
                    Debug.Log("[Setup] PlayerProgressManager component already exists on GameManager GameObject.");
                }

                // Add RunPerkManager to [GameManager] as well
                RunPerkManager rpm = gmObj.GetComponent<RunPerkManager>();
                if (rpm == null)
                {
                    rpm = gmObj.AddComponent<RunPerkManager>();
                    Undo.RegisterCreatedObjectUndo(rpm, "Add RunPerkManager");
                    EditorUtility.SetDirty(gmObj);
                    Debug.Log("[Setup] Added RunPerkManager component to GameManager GameObject.");
                }
                else
                {
                    Debug.Log("[Setup] RunPerkManager component already exists on GameManager GameObject.");
                }
            }
            else
            {
                Debug.LogError("[Setup] GameManager GameObject not found in the scene.");
            }

            // 2. Add PlayerProgressUI to [MainMenuPanel] and PerkChoiceUI to Canvas
            GameObject panelObj = GameObject.Find("MainMenuPanel");
            if (panelObj != null)
            {
                PlayerProgressUI ppUI = panelObj.GetComponent<PlayerProgressUI>();
                if (ppUI == null)
                {
                    ppUI = panelObj.AddComponent<PlayerProgressUI>();
                    Undo.RegisterCreatedObjectUndo(ppUI, "Add PlayerProgressUI");
                    EditorUtility.SetDirty(panelObj);
                    Debug.Log("[Setup] Added PlayerProgressUI component to MainMenuPanel GameObject.");
                }
                else
                {
                    Debug.Log("[Setup] PlayerProgressUI component already exists on MainMenuPanel GameObject.");
                }
            }
            else
            {
                Debug.LogError("[Setup] MainMenuPanel GameObject not found in the scene.");
            }

            // Set up PerkChoiceUI component on the Canvas
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
            {
                PerkChoiceUI perkUI = canvasObj.GetComponent<PerkChoiceUI>();
                if (perkUI == null)
                {
                    perkUI = canvasObj.AddComponent<PerkChoiceUI>();
                    Undo.RegisterCreatedObjectUndo(perkUI, "Add PerkChoiceUI");
                    EditorUtility.SetDirty(canvasObj);
                    Debug.Log("[Setup] Added PerkChoiceUI component to Canvas GameObject.");
                }
                else
                {
                    Debug.Log("[Setup] PerkChoiceUI component already exists on Canvas.");
                }
            }
            else
            {
                Debug.LogError("[Setup] Canvas GameObject not found in the scene.");
            }

            // 3. Set up in-game HUD progress text
            GameObject hudBar = GameObject.Find("HUDTopBar");
            if (hudBar != null)
            {
                Transform existingText = hudBar.transform.Find("HUDProgressText");
                TMPro.TextMeshProUGUI tmpText = null;
                if (existingText != null)
                {
                    tmpText = existingText.GetComponent<TMPro.TextMeshProUGUI>();
                    // Force correct positioning for existing component
                    RectTransform rt = existingText.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(0.15f, 0.50f);
                        rt.anchorMax = new Vector2(0.15f, 0.50f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.anchoredPosition = new Vector2(150f, 0f);
                        rt.sizeDelta = new Vector2(200f, 50f);
                        EditorUtility.SetDirty(existingText.gameObject);
                    }
                }
                else
                {
                    GameObject textObj = new GameObject("HUDProgressText", typeof(RectTransform));
                    textObj.transform.SetParent(hudBar.transform, false);
                    
                    RectTransform rt = textObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.15f, 0.50f);
                    rt.anchorMax = new Vector2(0.15f, 0.50f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(150f, 0f);
                    rt.sizeDelta = new Vector2(200f, 50f);

                    tmpText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
                    
                    // Copy styling from HPText or WaveText if possible
                    Transform sourceTextObj = hudBar.transform.Find("HPText");
                    if (sourceTextObj == null) sourceTextObj = hudBar.transform.Find("WaveText");
                    if (sourceTextObj != null)
                    {
                        TMPro.TextMeshProUGUI source = sourceTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                        if (source != null)
                        {
                            tmpText.font = source.font;
                            tmpText.fontSize = source.fontSize;
                            tmpText.color = source.color;
                            tmpText.alignment = TMPro.TextAlignmentOptions.Center;
                        }
                    }
                    else
                    {
                        tmpText.fontSize = 20f;
                        tmpText.color = Color.white;
                        tmpText.alignment = TMPro.TextAlignmentOptions.Center;
                    }

                    Undo.RegisterCreatedObjectUndo(textObj, "Create HUDProgressText");
                    Debug.Log("[Setup] Created HUDProgressText in HUDTopBar.");
                }

                // Wire to UIManager
                UIManager uiManager = Object.FindFirstObjectByType<UIManager>();
                if (uiManager != null)
                {
                    uiManager.hudProgressText = tmpText;
                    EditorUtility.SetDirty(uiManager);
                    Debug.Log("[Setup] Assigned hudProgressText to UIManager.");
                }
            }
            else
            {
                Debug.LogWarning("[Setup] HUDTopBar not found, skipping HUD text configuration.");
            }

            // Save the scene
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Setup] Player Progress setup completed and scene saved successfully!");
        }
    }
}
#endif
