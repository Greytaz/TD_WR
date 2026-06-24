#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TowerDefense.Data;
using TowerDefense.Towers;
using TowerDefense.Projectiles;
using TowerDefense.UI;

namespace TowerDefense.Editor
{
    public static class TowerRenameSetup
    {
        [MenuItem("Tower Defense/Rename and Redesign Towers")]
        public static void RenameAndRedesign()
        {
            // Make sure active scene is loaded
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "SampleScene")
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
                }
            }

            Debug.Log("[Setup] Starting automated tower rename & redesign setup...");

            // 1. Rename and update TowerData ScriptableObject assets
            UpdateTowerAsset("Assets/Data/Towers/ArcherData.asset", "Assets/Data/Towers/AssaultData.asset", "Assault", TowerType.Assault);
            UpdateTowerAsset("Assets/Data/Towers/CannonData.asset", "Assets/Data/Towers/CommandData.asset", "Command", TowerType.Command);
            UpdateTowerAsset("Assets/Data/Towers/MageData.asset", "Assets/Data/Towers/EnergyData.asset", "Energy", TowerType.Energy);

            // 2. Load and modify the spawned projectile prefabs
            UpdateCannonballPrefab();
            UpdateMageSpherePrefab();

            // 3. Rename Canvas UI shop buttons in the scene
            UpdateSceneButtons();

            // Save and refresh database
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Setup] Automated tower rename & redesign setup completed successfully!");
        }

        private static void UpdateTowerAsset(string oldPath, string newPath, string newName, TowerType newType)
        {
            TowerData asset = AssetDatabase.LoadAssetAtPath<TowerData>(oldPath);
            if (asset != null)
            {
                asset.towerName = newName;
                asset.towerType = newType;
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();

                // If path needs to be changed
                if (oldPath != newPath)
                {
                    string error = AssetDatabase.RenameAsset(oldPath, newName + "Data");
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"[Setup] Failed to rename asset from {oldPath} to {newName}Data. Error: {error}");
                    }
                    else
                    {
                        Debug.Log($"[Setup] Renamed and updated asset {oldPath} -> {newPath}.");
                    }
                }
            }
            else
            {
                // Try checking at new path if already renamed
                TowerData alreadyRenamed = AssetDatabase.LoadAssetAtPath<TowerData>(newPath);
                if (alreadyRenamed != null)
                {
                    alreadyRenamed.towerName = newName;
                    alreadyRenamed.towerType = newType;
                    EditorUtility.SetDirty(alreadyRenamed);
                    Debug.Log($"[Setup] Asset at {newPath} already exists and is updated.");
                }
                else
                {
                    Debug.LogWarning($"[Setup] Asset not found at {oldPath} or {newPath}!");
                }
            }
        }

        private static void UpdateCannonballPrefab()
        {
            string prefabPath = "Assets/Prefabs/Projectiles/Cannonball.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                // Remove SplashProjectile, add RocketProjectile
                var splash = prefab.GetComponent<SplashProjectile>();
                if (splash != null)
                {
                    Object.DestroyImmediate(splash, true);
                }

                var rocket = prefab.GetComponent<RocketProjectile>();
                if (rocket == null)
                {
                    rocket = prefab.AddComponent<RocketProjectile>();
                    rocket.poolTag = "Cannonball"; // Keep tag same to avoid re-wiring pools unless needed
                }

                EditorUtility.SetDirty(prefab);
                Debug.Log($"[Setup] Upgraded prefab {prefabPath} to use RocketProjectile.");
            }
            else
            {
                Debug.LogError($"[Setup] Prefab not found at {prefabPath}!");
            }
        }

        private static void UpdateMageSpherePrefab()
        {
            string prefabPath = "Assets/Prefabs/Projectiles/MageSphere.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                // Remove HomingProjectile, add EnergyBeamProjectile
                var homing = prefab.GetComponent<HomingProjectile>();
                if (homing != null)
                {
                    Object.DestroyImmediate(homing, true);
                }

                var beam = prefab.GetComponent<EnergyBeamProjectile>();
                if (beam == null)
                {
                    beam = prefab.AddComponent<EnergyBeamProjectile>();
                    beam.poolTag = "MageSphere"; // Keep tag same to avoid re-wiring pools unless needed
                }

                // Make the visual mesh renderer of MageSphere inactive so the laser looks like an actual beam line without a flying sphere
                var mr = prefab.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.enabled = false;
                }
                else
                {
                    var childMr = prefab.GetComponentInChildren<MeshRenderer>();
                    if (childMr != null) childMr.enabled = false;
                }

                EditorUtility.SetDirty(prefab);
                Debug.Log($"[Setup] Upgraded prefab {prefabPath} to use EnergyBeamProjectile and hid sphere mesh.");
            }
            else
            {
                Debug.LogError($"[Setup] Prefab not found at {prefabPath}!");
            }
        }

        private static void UpdateSceneButtons()
        {
            // 1. ArcherButton -> AssaultButton
            GameObject archerBtn = GameObject.Find("Canvas/ShopBottomPanel/ArcherButton");
            if (archerBtn != null)
            {
                archerBtn.name = "AssaultButton";
                var label = archerBtn.transform.Find("Label")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = "Assault";
                    EditorUtility.SetDirty(label);
                }
                EditorUtility.SetDirty(archerBtn);
                Debug.Log("[Setup] Scene update: Renamed ArcherButton to AssaultButton and updated text to 'Assault'.");
            }

            // 2. CannonButton -> CommandButton
            GameObject cannonBtn = GameObject.Find("Canvas/ShopBottomPanel/CannonButton");
            if (cannonBtn != null)
            {
                cannonBtn.name = "CommandButton";
                var label = cannonBtn.transform.Find("Label")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = "Command";
                    EditorUtility.SetDirty(label);
                }
                EditorUtility.SetDirty(cannonBtn);
                Debug.Log("[Setup] Scene update: Renamed CannonButton to CommandButton and updated text to 'Command'.");
            }

            // 3. MageButton -> EnergyButton
            GameObject mageBtn = GameObject.Find("Canvas/ShopBottomPanel/MageButton");
            if (mageBtn != null)
            {
                mageBtn.name = "EnergyButton";
                var label = mageBtn.transform.Find("Label")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = "Energy";
                    EditorUtility.SetDirty(label);
                }
                EditorUtility.SetDirty(mageBtn);
                Debug.Log("[Setup] Scene update: Renamed MageButton to EnergyButton and updated text to 'Energy'.");
            }
        }
    }
}
#endif
