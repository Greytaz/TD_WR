using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using TowerDefense.UI;
using TowerDefense.Core;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";
        private const string ScriptPathKey = "PlayModeTest.ScriptPath";
        private const string SentinelLog = "PLAY_MODE_TEST_COMPLETE";

        private static readonly int WaitFrames = SessionState.GetInt("PlayModeTest.WaitFrames", 15);
        private static readonly float TestTimeout = SessionState.GetFloat("PlayModeTest.TestTimeout", 15.0f);

        // Log capture
        private static List<string> _capturedLogs = new List<string>();
        private const int MaxCapturedLogs = 50;

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");

            switch (state)
            {
                case "Idle":
                    break;

                case "WaitingForCompile":
                    Debug.Log("[PlayModeTest] Bootstrap compiled. Scheduling Play Mode entry.");
                    EditorApplication.delayCall += () =>
                    {
                        SessionState.SetString(StateKey, "EnteringPlayMode");
                        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                        EditorApplication.isPlaying = true;
                    };
                    break;

                case "EnteringPlayMode":
                    EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                        SessionState.SetString(StateKey, "InPlayMode");
                        EditorApplication.update += WaitFramesThenRun;
                    }
                    break;

                case "InPlayMode":
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.update += WaitFramesThenRun;
                    }
                    break;

                case "Done":
                    Debug.Log(SentinelLog);
                    EditorApplication.delayCall += SelfDestruct;
                    break;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                SessionState.SetString(StateKey, "InPlayMode");
                EditorApplication.update += WaitFramesThenRun;
            }
        }

        private static int _frameCount = 0;
        private static bool _setupDone = false;
        private static bool _testDone = false;
        private static double _testStartTime = 0;

        private static void WaitFramesThenRun()
        {
            _frameCount++;
            if (_frameCount < WaitFrames) return;

            if (_testDone) return;

            // Start log capture
            if (!_setupDone)
            {
                _setupDone = true;
                Application.logMessageReceived += OnLogMessage;
                _testStartTime = EditorApplication.timeSinceStartup;
                try
                {
                    Setup();
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[PlayModeTest] Setup threw exception: " + e);
                    FinishTest(true, e.Message);
                    return;
                }
                return; // Let one frame pass after Setup before first Tick
            }

            // Tick every frame
            float elapsed = (float)(EditorApplication.timeSinceStartup - _testStartTime);
            bool timedOut = elapsed >= TestTimeout;

            try
            {
                bool complete = Tick(elapsed);
                if (complete || timedOut)
                {
                    if (timedOut && !complete)
                    {
                        Debug.LogWarning("[PlayModeTest] Test timed out after " + elapsed + "s");
                    }
                    FinishTest(timedOut && !complete, timedOut ? "Test timed out after " + TestTimeout + "s" : null);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[PlayModeTest] Tick threw exception: " + e);
                FinishTest(true, e.Message);
            }
        }

        private static void FinishTest(bool isError, string errorMessage)
        {
            _testDone = true;
            EditorApplication.update -= WaitFramesThenRun;
            Application.logMessageReceived -= OnLogMessage;

            string resultJson;
            try
            {
                resultJson = GetResult();
            }
            catch (System.Exception e)
            {
                resultJson = JsonUtility.ToJson(new TestResult
                {
                    success = false,
                    error = "GetResult() threw: " + e.Message,
                    logs = _capturedLogs.ToArray()
                });
            }

            // Inject logs and error info if needed
            if (isError && errorMessage != null)
            {
                resultJson = JsonUtility.ToJson(new TestResult
                {
                    success = false,
                    error = errorMessage,
                    logs = _capturedLogs.ToArray()
                });
            }

            SessionState.SetString(ResultKey, resultJson);
            SessionState.SetString(StateKey, "Done");
            EditorApplication.isPlaying = false;
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (_capturedLogs.Count >= MaxCapturedLogs) return;
            if (type == LogType.Error || type == LogType.Exception ||
                message.Contains("[Test]") || message.Contains("TEST_RESULT"))
            {
                _capturedLogs.Add("[" + type + "] " + message);
            }
        }

        private static void SelfDestruct()
        {
            string scriptPath = SessionState.GetString(ScriptPathKey, "");
            if (!string.IsNullOrEmpty(scriptPath) && AssetDatabase.AssetPathExists(scriptPath))
            {
                AssetDatabase.DeleteAsset(scriptPath);
            }
            SessionState.EraseString(StateKey);
            SessionState.EraseString(ScriptPathKey);
        }

        // ============================================================
        // RESULT CLASS
        // ============================================================
        [System.Serializable]
        private class TestResult
        {
            public bool success;
            public string error;
            public string[] logs;
        }

        private static int _step = 0;
        private static bool _success = false;
        private static string _errorMsg = "";
        private static float _lastElapsed = 0f;

        private static void Setup()
        {
            Debug.Log("[Test] Setup complete, waiting for first active frame.");
        }

        private static bool Tick(float elapsed)
        {
            if (!string.IsNullOrEmpty(_errorMsg)) return true;

            // Wait until the game loop is actually running and has processed at least one frame
            if (Time.frameCount < 2) return false;

            // Wait a few frames for events and state change to propagate
            if (elapsed - _lastElapsed < 0.5f) return false;
            _lastElapsed = elapsed;

            _step++;
            Debug.LogFormat("[Test] Tick step {0}, state: {1}, timeScale: {2}, frameCount: {3}", _step, GameManager.Instance.CurrentState, Time.timeScale, Time.frameCount);

            var ui = Object.FindFirstObjectByType<UIManager>();
            if (ui == null)
            {
                _errorMsg = "UIManager not found";
                return true;
            }

            if (_step == 1)
            {
                // Verify we are initially in MainMenu state
                if (GameManager.Instance.CurrentState != GameState.MainMenu)
                {
                    _errorMsg = "Expected state MainMenu in step 1, but got " + GameManager.Instance.CurrentState;
                    return true;
                }

                if (ui.startButton != null)
                {
                    ui.startButton.onClick.Invoke();
                    Debug.Log("[Test] Step 1: Clicked startButton");
                }
                else
                {
                    _errorMsg = "startButton is null";
                    return true;
                }
            }
            else if (_step == 2)
            {
                // Verify we are now in Playing state with Time.timeScale = 1
                if (GameManager.Instance.CurrentState != GameState.Playing || Time.timeScale != 1f)
                {
                    _errorMsg = "Expected state Playing and timeScale 1 in step 2, but got state=" + GameManager.Instance.CurrentState + " timeScale=" + Time.timeScale;
                    return true;
                }

                // Force Trigger Game Over
                Debug.Log("[Test] Step 2: Triggering GameOver on GameManager");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.GameOver();
                }
                else
                {
                    _errorMsg = "GameManager.Instance is null";
                    return true;
                }
            }
            else if (_step == 3)
            {
                // Verify we are in GameOver state with Time.timeScale = 0 and GameOverPanel is active
                if (GameManager.Instance.CurrentState != GameState.GameOver || Time.timeScale != 0f)
                {
                    _errorMsg = "Expected state GameOver and timeScale 0 in step 3, but got state=" + GameManager.Instance.CurrentState + " timeScale=" + Time.timeScale;
                    return true;
                }

                if (ui.gameOverPanel == null || !ui.gameOverPanel.activeSelf)
                {
                    _errorMsg = "Expected gameOverPanel to be active in step 3";
                    return true;
                }

                if (ui.gameOverMainMenuButton == null || !ui.gameOverMainMenuButton.gameObject.activeInHierarchy)
                {
                    _errorMsg = "Expected gameOverMainMenuButton to be active in hierarchy in step 3";
                    return true;
                }

                if (ui.gameOverQuitButton == null || !ui.gameOverQuitButton.gameObject.activeInHierarchy)
                {
                    _errorMsg = "Expected gameOverQuitButton to be active in hierarchy in step 3";
                    return true;
                }

                // Click Game Over Main Menu button to return to Main Menu
                Debug.Log("[Test] Step 3: Clicking gameOverMainMenuButton");
                ui.gameOverMainMenuButton.onClick.Invoke();
            }
            else if (_step == 4)
            {
                // Verify we are back in MainMenu state with Time.timeScale = 0 and MainMenuPanel is active
                if (GameManager.Instance.CurrentState != GameState.MainMenu || Time.timeScale != 0f)
                {
                    _errorMsg = "Expected state MainMenu and timeScale 0 in step 4, but got state=" + GameManager.Instance.CurrentState + " timeScale=" + Time.timeScale;
                    return true;
                }

                if (ui.mainMenuPanel == null || !ui.mainMenuPanel.activeSelf)
                {
                    _errorMsg = "Expected mainMenuPanel to be active in step 4";
                    return true;
                }

                if (ui.gameOverPanel != null && ui.gameOverPanel.activeSelf)
                {
                    _errorMsg = "Expected gameOverPanel to be inactive in step 4";
                    return true;
                }

                _success = true;
                return true; // Test passed!
            }

            return false;
        }

        private static string GetResult()
        {
            var result = new TestResult
            {
                success = _success,
                error = _errorMsg,
                logs = _capturedLogs.ToArray()
            };
            return JsonUtility.ToJson(result);
        }
    }
}