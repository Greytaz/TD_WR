#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using TowerDefense.Data;
using TowerDefense.Core;
using TowerDefense.Towers;
using TowerDefense.UI;
using TowerDefense.Utils;
using TowerDefense.Enemies;
using TowerDefense.Projectiles;

namespace TowerDefense.Editor
{
    public static class SetupHelper
    {
        [MenuItem("Tower Defense/Create Default Assets & Folders")]
        public static void CreateDefaultAssetsAndFolders()
        {
            // 1. Create directories
            string[] folders = new string[]
            {
                "Assets/Data",
                "Assets/Data/Towers",
                "Assets/Data/Enemies",
                "Assets/Data/Waves",
                "Assets/Prefabs",
                "Assets/Prefabs/Towers",
                "Assets/Prefabs/Enemies",
                "Assets/Prefabs/Projectiles",
                "Assets/Prefabs/Particles",
                "Assets/Materials",
                "Assets/Shaders"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
                    string sub = Path.GetFileName(folder);
                    AssetDatabase.CreateFolder(parent, sub);
                }
            }

            AssetDatabase.Refresh();

            // 2. Generate standard materials
            Shader pipelineShader = GetPipelineShader();
            Material redMat = CreateMaterial("RedMat", new Color(1f, 0.2f, 0.2f), pipelineShader);
            Material orangeMat = CreateMaterial("OrangeMat", new Color(1f, 0.5f, 0f), pipelineShader);
            Material yellowMat = CreateMaterial("YellowMat", new Color(1f, 0.9f, 0.1f), pipelineShader);
            Material grayMat = CreateMaterial("GrayMat", new Color(0.5f, 0.5f, 0.5f), pipelineShader);
            Material blueMat = CreateMaterial("BlueMat", new Color(0.2f, 0.6f, 1f), pipelineShader);
            Material purpleMat = CreateMaterial("PurpleMat", new Color(0.6f, 0.2f, 0.8f), pipelineShader);
            Material greenMat = CreateMaterial("GreenMat", new Color(0.2f, 0.8f, 0.2f), pipelineShader);
            Material blackMat = CreateMaterial("BlackMat", new Color(0.1f, 0.1f, 0.1f), pipelineShader);

            // 3. Create default indicators
            GameObject cellHighlight = CreateCellHighlightPrefab(greenMat);
            GameObject rangeIndicator = CreateRangeIndicatorPrefab();

            // 4. Create projectiles
            GameObject arrowProj = CreateHomingProjectilePrefab("Arrow", yellowMat, false);
            GameObject mageProj = CreateHomingProjectilePrefab("MageSphere", purpleMat, true);
            GameObject cannonProj = CreateSplashProjectilePrefab("Cannonball", blackMat);

            // 5. Create default enemies
            EnemyData lightEnemy = CreateEnemyData("LightEnemy", "Light", EnemyType.Light, 50f, 3.0f, 5, 1f, 1f, 1f);
            EnemyData heavyEnemy = CreateEnemyData("HeavyEnemy", "Heavy", EnemyType.Heavy, 150f, 1.5f, 10, 1f, 0.75f, 1f);
            EnemyData fastEnemy = CreateEnemyData("FastEnemy", "Fast", EnemyType.Fast, 40f, 4.5f, 6, 1f, 1f, 1f);
            EnemyData armoredEnemy = CreateEnemyData("ArmoredEnemy", "Armored", EnemyType.Armored, 100f, 2.0f, 7, 0.5f, 1f, 1f);
            EnemyData bossEnemy = CreateEnemyData("BossEnemy", "Boss", EnemyType.Boss, 1000f, 1.2f, 25, 1f, 1f, 1f);

            GameObject lightEnemyPrefab = CreateEnemyPrefab("LightEnemy", redMat, lightEnemy, PrimitiveType.Capsule, new Vector3(0.5f, 0.5f, 0.5f));
            GameObject heavyEnemyPrefab = CreateEnemyPrefab("HeavyEnemy", orangeMat, heavyEnemy, PrimitiveType.Capsule, new Vector3(0.7f, 0.7f, 0.7f));
            GameObject fastEnemyPrefab = CreateEnemyPrefab("FastEnemy", yellowMat, fastEnemy, PrimitiveType.Capsule, new Vector3(0.4f, 0.4f, 0.4f));
            GameObject armoredEnemyPrefab = CreateEnemyPrefab("ArmoredEnemy", grayMat, armoredEnemy, PrimitiveType.Cylinder, new Vector3(0.5f, 0.5f, 0.5f));
            GameObject bossEnemyPrefab = CreateEnemyPrefab("BossEnemy", blackMat, bossEnemy, PrimitiveType.Cube, new Vector3(1.25f, 1.25f, 1.25f));

            // Link prefabs to scriptables
            lightEnemy.prefab = lightEnemyPrefab;
            heavyEnemy.prefab = heavyEnemyPrefab;
            fastEnemy.prefab = fastEnemyPrefab;
            armoredEnemy.prefab = armoredEnemyPrefab;
            bossEnemy.prefab = bossEnemyPrefab;

            EditorUtility.SetDirty(lightEnemy);
            EditorUtility.SetDirty(heavyEnemy);
            EditorUtility.SetDirty(fastEnemy);
            EditorUtility.SetDirty(armoredEnemy);
            EditorUtility.SetDirty(bossEnemy);

            // 6. Create default tower assets
            // ARCHER
            TowerData archer = ScriptableObject.CreateInstance<TowerData>();
            archer.towerName = "Archer";
            archer.towerType = TowerType.Assault;
            archer.tier1 = new TowerTierData { cost = 100, damage = 10, range = 6f, fireRate = 2.5f, projectileSpeed = 15f };
            archer.tier2 = new TowerTierData { cost = 120, damage = 16, range = 7f, fireRate = 2.8f, projectileSpeed = 17f };
            archer.tier3 = new TowerTierData { cost = 150, damage = 25, range = 8f, fireRate = 3.2f, projectileSpeed = 20f };
            GameObject archerPrefab = CreateTowerPrefab("ArcherTower", yellowMat, archer, "Arrow");
            archer.prefab = archerPrefab;
            SaveAsset(archer, "Assets/Data/Towers/ArcherData.asset");

            // CANNON
            TowerData cannon = ScriptableObject.CreateInstance<TowerData>();
            cannon.towerName = "Cannon";
            cannon.towerType = TowerType.Command;
            cannon.tier1 = new TowerTierData { cost = 150, damage = 35, range = 5f, fireRate = 0.8f, projectileSpeed = 10f, splashRadius = 2.5f };
            cannon.tier2 = new TowerTierData { cost = 180, damage = 55, range = 5.5f, fireRate = 0.9f, projectileSpeed = 11f, splashRadius = 3f };
            cannon.tier3 = new TowerTierData { cost = 220, damage = 85, range = 6f, fireRate = 1.0f, projectileSpeed = 12f, splashRadius = 3.5f, burnDamagePerSecond = 10f, burnDuration = 3f };
            GameObject cannonPrefab = CreateTowerPrefab("CannonTower", blackMat, cannon, "Cannonball");
            cannon.prefab = cannonPrefab;
            SaveAsset(cannon, "Assets/Data/Towers/CannonData.asset");

            // MAGE
            TowerData mage = ScriptableObject.CreateInstance<TowerData>();
            mage.towerName = "Mage";
            mage.towerType = TowerType.Energy;
            mage.tier1 = new TowerTierData { cost = 120, damage = 15, range = 5.5f, fireRate = 1.2f, projectileSpeed = 12f, slowFactor = 0.5f, slowDuration = 2f };
            mage.tier2 = new TowerTierData { cost = 140, damage = 24, range = 6.5f, fireRate = 1.4f, projectileSpeed = 14f, slowFactor = 0.4f, slowDuration = 2.5f };
            mage.tier3 = new TowerTierData { cost = 180, damage = 40, range = 7.5f, fireRate = 1.6f, projectileSpeed = 16f, slowFactor = 0.3f, slowDuration = 3f, stunDuration = 1f, stunChance = 0.25f };
            GameObject magePrefab = CreateTowerPrefab("MageTower", purpleMat, mage, "MageSphere");
            mage.prefab = magePrefab;
            SaveAsset(mage, "Assets/Data/Towers/MageData.asset");

            // 7. Create default wave assets
            WaveData wave1 = ScriptableObject.CreateInstance<WaveData>();
            wave1.waveName = "Wave 1: First Contact";
            wave1.healthMultiplier = 1.0f;
            wave1.speedMultiplier = 1.0f;
            wave1.waveBonusGold = 50;
            wave1.spawnGroups = new List<EnemySpawnGroup>
            {
                new EnemySpawnGroup { enemyData = lightEnemy, count = 15, spawnInterval = 1.5f }
            };
            SaveAsset(wave1, "Assets/Data/Waves/Wave1.asset");

            WaveData wave2 = ScriptableObject.CreateInstance<WaveData>();
            wave2.waveName = "Wave 2: Accelerated Threat";
            wave2.healthMultiplier = 1.1f;
            wave2.speedMultiplier = 1.0f;
            wave2.waveBonusGold = 60;
            wave2.spawnGroups = new List<EnemySpawnGroup>
            {
                new EnemySpawnGroup { enemyData = lightEnemy, count = 20, spawnInterval = 1.2f },
                new EnemySpawnGroup { enemyData = fastEnemy, count = 10, spawnInterval = 1.0f }
            };
            SaveAsset(wave2, "Assets/Data/Waves/Wave2.asset");

            // 8. Generate basic particle effect prefabs (spawners for ObjectPool)
            GameObject impactBurst = CreateParticleSystemPrefab("ImpactBurst", Color.yellow);
            GameObject deathBurst = CreateParticleSystemPrefab("DeathBurst", Color.red);
            GameObject placementDust = CreateParticleSystemPrefab("PlacementDust", Color.gray);
            GameObject upgradeBurst = CreateParticleSystemPrefab("UpgradeBurst", Color.green);
            GameObject sellDust = CreateParticleSystemPrefab("SellDust", Color.cyan);

            // 9. Auto-wire everything on the currently opened SampleScene!
            WireSceneElements(archer, cannon, mage, wave1, wave2, rangeIndicator, cellHighlight);

            AssetDatabase.Refresh();
            Debug.Log("Successfully created ALL default game prefabs, wired scriptable data, and auto-configured the Active Scene components! Ready to play.");
        }

        private static Shader GetPipelineShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Lightweight Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            return s;
        }

        private static Material CreateMaterial(string name, Color color, Shader shader)
        {
            string path = $"Assets/Materials/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.color = color;
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
                mat.color = color;
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static GameObject CreateCellHighlightPrefab(Material greenMat)
        {
            string path = "Assets/Prefabs/CellHighlight.prefab";
            GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
            cell.name = "CellHighlight";
            cell.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            
            var col = cell.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var r = cell.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = greenMat;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cell, path);
            Object.DestroyImmediate(cell);
            return prefab;
        }

        private static GameObject CreateRangeIndicatorPrefab()
        {
            string path = "Assets/Prefabs/RangeIndicator.prefab";
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "RangeIndicator";
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var col = ring.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var r = ring.GetComponent<Renderer>();
            if (r != null)
            {
                Shader shader = Shader.Find("Custom/RangeIndicator");
                if (shader != null)
                {
                    string matPath = "Assets/Materials/RangeIndicatorMat.mat";
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null)
                    {
                        mat = new Material(shader);
                        AssetDatabase.CreateAsset(mat, matPath);
                    }
                    r.sharedMaterial = mat;
                }
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(ring, path);
            Object.DestroyImmediate(ring);
            return prefab;
        }

        private static GameObject CreateHomingProjectilePrefab(string name, Material mat, bool isElem)
        {
            string path = $"Assets/Prefabs/Projectiles/{name}.prefab";
            GameObject proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proj.name = name;
            proj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            var r = proj.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;

            var col = proj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var hp = proj.AddComponent<HomingProjectile>();
            hp.poolTag = name;
            hp.isElemental = isElem;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(proj, path);
            Object.DestroyImmediate(proj);
            return prefab;
        }

        private static GameObject CreateSplashProjectilePrefab(string name, Material mat)
        {
            string path = $"Assets/Prefabs/Projectiles/{name}.prefab";
            GameObject proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proj.name = name;
            proj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            var r = proj.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;

            var col = proj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var sp = proj.AddComponent<SplashProjectile>();
            sp.poolTag = name;
            sp.arcHeight = 2.0f;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(proj, path);
            Object.DestroyImmediate(proj);
            return prefab;
        }

        private static GameObject CreateEnemyPrefab(string name, Material mat, EnemyData data, PrimitiveType type, Vector3? customScale = null)
        {
            string path = $"Assets/Prefabs/Enemies/{name}.prefab";
            GameObject enemy = GameObject.CreatePrimitive(type);
            enemy.name = name;
            enemy.transform.localScale = customScale ?? Vector3.one;

            var r = enemy.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;

            // Rigidbody
            var rb = enemy.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            // Health bar billboard anchor
            GameObject hbCanvas = new GameObject("HealthBarBillboard", typeof(Canvas), typeof(TowerDefense.Effects.Billboard));
            var canvas = hbCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            hbCanvas.transform.SetParent(enemy.transform, false);
            hbCanvas.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            hbCanvas.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);

            GameObject hbBg = new GameObject("HealthBarBackground");
            hbBg.transform.SetParent(hbCanvas.transform, false);
            var bgRect = hbBg.AddComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(60f, 8f);
            var bgImg = hbBg.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = Color.black;

            GameObject hbFg = new GameObject("HealthBarForeground");
            hbFg.transform.SetParent(hbBg.transform, false);
            var fgRect = hbFg.AddComponent<RectTransform>();
            fgRect.anchorMin = new Vector2(0f, 0.5f);
            fgRect.anchorMax = new Vector2(0f, 0.5f);
            fgRect.pivot = new Vector2(0f, 0.5f);
            fgRect.sizeDelta = new Vector2(60f, 8f);
            fgRect.anchoredPosition = Vector2.zero;
            var fgImg = hbFg.AddComponent<UnityEngine.UI.Image>();
            fgImg.color = Color.green;

            // Scripts
            var mv = enemy.AddComponent<EnemyMovement>();
            var hl = enemy.AddComponent<EnemyHealth>();
            hl.enemyData = data;
            hl.healthBarForeground = fgRect;

            var baseComp = enemy.AddComponent<EnemyBase>();
            baseComp.enemyData = data;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, path);
            Object.DestroyImmediate(enemy);
            return prefab;
        }

        private static GameObject CreateTowerPrefab(string name, Material mat, TowerData data, string projTag)
        {
            string path = $"Assets/Prefabs/Towers/{name}.prefab";
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = name;
            tower.transform.localScale = new Vector3(1f, 1f, 1f);

            var r = tower.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;

            // Create Rotatable Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "RotatableHead";
            head.transform.SetParent(tower.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            head.transform.localScale = new Vector3(0.8f, 0.6f, 0.8f);
            var headRenderer = head.GetComponent<Renderer>();
            if (headRenderer != null) headRenderer.sharedMaterial = mat;

            // Fire Point
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(head.transform, false);
            firePoint.transform.localPosition = new Vector3(0f, 0f, 0.6f);

            // Base Script
            var tb = tower.AddComponent<TowerBase>();
            tb.towerData = data;
            tb.rotatableHead = head.transform;
            tb.firePoint = firePoint.transform;
            tb.projectilePoolTag = projTag;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tower, path);
            Object.DestroyImmediate(tower);
            return prefab;
        }

        private static GameObject CreateParticleSystemPrefab(string name, Color color)
        {
            string path = $"Assets/Prefabs/Particles/{name}.prefab";
            GameObject obj = new GameObject(name);
            
            var ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1.0f;
            main.loop = false;
            main.startColor = color;
            main.startSpeed = 5.0f;
            main.startSize = 0.2f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            var burst = new ParticleSystem.Burst(0.0f, 15);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);
            return prefab;
        }

        private static EnemyData CreateEnemyData(string filename, string name, EnemyType type, float health, float speed, int reward, float phys, float exp, float elem)
        {
            string path = $"Assets/Data/Enemies/{filename}.asset";
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyData>();
            }
            data.enemyName = name;
            data.enemyType = type;
            data.maxHealth = health;
            data.speed = speed;
            data.goldReward = reward;
            data.physicalResistance = phys;
            data.explosiveResistance = exp;
            data.elementalResistance = elem;
            SaveAsset(data, path);
            return data;
        }

        private static void WireSceneElements(TowerData archer, TowerData cannon, TowerData mage, WaveData wave1, WaveData wave2, GameObject rangeInd, GameObject highlight)
        {
            // Find Managers
            var gridObj = GameObject.Find("GridManager");
            if (gridObj != null)
            {
                var gridManager = gridObj.GetComponent<GridManager>();
                if (gridManager != null && (gridManager.pathWaypoints == null || gridManager.pathWaypoints.Count == 0))
                {
                    gridManager.pathWaypoints = new List<Vector3>
                    {
                        new Vector3(1.5f, 0f, 1.5f),
                        new Vector3(1.5f, 0f, 10.5f),
                        new Vector3(13.5f, 0f, 10.5f),
                        new Vector3(13.5f, 0f, 21.0f)
                    };
                    EditorUtility.SetDirty(gridManager);
                }
            }

            var wmObj = GameObject.Find("WaveManager");
            if (wmObj != null)
            {
                var waveManager = wmObj.GetComponent<WaveManager>();
                if (waveManager != null)
                {
                    waveManager.preconfiguredWaves = new List<WaveData> { wave1, wave2 };
                    EditorUtility.SetDirty(waveManager);
                }
            }

            var opObj = GameObject.Find("ObjectPool");
            if (opObj != null)
            {
                var pool = opObj.GetComponent<ObjectPool>();
                if (pool != null)
                {
                    pool.pools = new List<ObjectPool.Pool>
                    {
                        new ObjectPool.Pool { tag = "Light", prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/LightEnemy.prefab"), size = 20 },
                        new ObjectPool.Pool { tag = "Heavy", prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/HeavyEnemy.prefab"), size = 15 },
                        new ObjectPool.Pool { tag = "Fast", prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/FastEnemy.prefab"), size = 15 },
                        new ObjectPool.Pool { tag = "Armored", prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/ArmoredEnemy.prefab"), size = 15 },
                        new ObjectPool.Pool { tag = "Boss", prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/BossEnemy.prefab"), size = 2 },
                        new ObjectPool.Pool { tag = "Arrow", prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/Arrow.prefab"), size = 40 },
                        new ObjectPool.Pool { tag = "MageSphere", prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/MageSphere.prefab"), size = 40 },
                        new ObjectPool.Pool { tag = "Cannonball", prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/Cannonball.prefab"), size = 25 },
                        new ObjectPool.Pool { tag = "ImpactBurst", prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Particles/ImpactBurst.prefab"), size = 20 },
                        new ObjectPool.Pool { tag = "DeathBurst", prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Particles/DeathBurst.prefab"), size = 15 },
                        new ObjectPool.Pool { tag = "PlacementDust", prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Particles/PlacementDust.prefab"), size = 10 },
                        new ObjectPool.Pool { tag = "UpgradeBurst", prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Particles/UpgradeBurst.prefab"), size = 10 },
                        new ObjectPool.Pool { tag = "SellDust", prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Particles/SellDust.prefab"), size = 10 },
                        new ObjectPool.Pool { tag = "DamageText", prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/DamageText.prefab"), size = 30 }
                    };
                    EditorUtility.SetDirty(pool);
                }
            }

            var tpObj = GameObject.Find("TowerPlacement");
            if (tpObj != null)
            {
                var placement = tpObj.GetComponent<TowerPlacement>();
                if (placement != null)
                {
                    placement.rangeIndicatorPrefab = rangeInd;
                    placement.cellHighlightPrefab = highlight;
                    EditorUtility.SetDirty(placement);
                }
            }

            // Bind tower data to UI buttons
            var archerBtn = GameObject.Find("ArcherButton");
            if (archerBtn != null)
            {
                var btn = archerBtn.GetComponent<TowerButton>();
                if (btn != null)
                {
                    btn.towerData = archer;
                    EditorUtility.SetDirty(btn);
                }
            }

            var cannonBtn = GameObject.Find("CannonButton");
            if (cannonBtn != null)
            {
                var btn = cannonBtn.GetComponent<TowerButton>();
                if (btn != null)
                {
                    btn.towerData = cannon;
                    EditorUtility.SetDirty(btn);
                }
            }

            var mageBtn = GameObject.Find("MageButton");
            if (mageBtn != null)
            {
                var btn = mageBtn.GetComponent<TowerButton>();
                if (btn != null)
                {
                    btn.towerData = mage;
                    EditorUtility.SetDirty(btn);
                }
            }

            // Save Scene
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
        }

        private static void SaveAsset(Object asset, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif

