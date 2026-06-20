using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using TowerDefense.Core;
using TowerDefense.Towers;
using TowerDefense.UI;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";
        private const string ScriptPathKey = "PlayModeTest.ScriptPath";
        private const string SentinelLog = "PLAY_MODE_TEST_COMPLETE";

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");

            switch (state)
            {
                case "Idle":
                    break;

                case "WaitingForCompile":
                    Debug.Log("[PlayModeTest] Bootstrap compiled.");
                    EditorApplication.delayCall += () =>
                    {
                        SessionState.SetString(StateKey, "EnteringPlayMode");
                        EditorApplication.isPlaying = true;
                    };
                    break;

                case "EnteringPlayMode":
                    if (EditorApplication.isPlaying)
                    {
                        SessionState.SetString(StateKey, "InPlayMode");
                        EditorApplication.update += RunTest;
                    }
                    break;
            }
        }

        private static int _frames = 0;

        private static void RunTest()
        {
            _frames++;
            if (_frames < 5) return;

            EditorApplication.update -= RunTest;

            // Load scene
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);

            // Click Archer button to spawn range indicator
            var archerBtn = GameObject.Find("ArcherButton");
            if (archerBtn != null)
            {
                var btn = archerBtn.GetComponent<TowerButton>();
                if (btn != null && btn.buttonComponent != null)
                {
                    btn.buttonComponent.onClick.Invoke();
                    Debug.Log("[Test] Clicked Archer Button.");
                }
            }

            // Find TowerPlacement spawned indicator
            var tpObj = GameObject.Find("TowerPlacement");
            if (tpObj != null)
            {
                var placement = tpObj.GetComponent<TowerPlacement>();
                if (placement != null)
                {
                    // Let's use reflection to get spawnedRangeIndicator
                    var field = typeof(TowerPlacement).GetField("spawnedRangeIndicator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        var indicator = field.GetValue(placement) as GameObject;
                        if (indicator != null)
                        {
                            // Activate it so we can read values
                            indicator.SetActive(true);
                            var renderer = indicator.GetComponentInChildren<Renderer>();
                            if (renderer != null)
                            {
                                var mat = renderer.material; // Access material instance
                                Debug.Log("[Test] Indicator active: " + indicator.activeSelf);
                                Debug.Log("[Test] Shader Name: " + mat.shader.name);
                                Debug.Log("[Test] Render Queue: " + mat.renderQueue);
                                Debug.Log("[Test] Material Color: " + mat.color);
                                if (mat.HasProperty("_Color"))
                                {
                                    Debug.Log("[Test] _Color property: " + mat.GetColor("_Color"));
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError("[Test] spawnedRangeIndicator is null!");
                        }
                    }
                }
            }

            SessionState.SetString(ResultKey, "Done");
            SessionState.SetString(StateKey, "Done");
            EditorApplication.isPlaying = false;
        }
    }
}
