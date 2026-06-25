# Project Overview 
- **Game Title**: Modular Tower Defense
- **High-Level Concept**: A strategic tower defense game where players build, upgrade, and customize defense towers to survive waves of enemies.
- **Players**: Single player
- **Target Platform**: PC & Mobile (Weak phones / Android & iOS)
- **Screen Orientation / Resolution**: Landscape (1920x1080)
- **Render Pipeline**: Universal Render Pipeline (URP)

# Game Mechanics 
## Core Gameplay Loop
The player places modular towers on a grid to intercept waves of enemies marching along a road. Surviving enemies reach the base and deduct player lives. The game continues through escalating waves until the player's lives reach 0, leading to a Game Over.

## Controls and Input Methods
Point-and-click mouse controls or touch taps for building/upgrading towers.

# UI
No UI changes are required. The optimization will happen entirely in the codebase, significantly reducing garbage collection (GC) pressure and preventing directory access crashes on mobile devices.

# Key Asset & Context
- **`Assets/Scripts/Enemies/EnemyHealth.cs`**: Handles enemy damage, lifetime, and initialization. We will introduce a static list `ActiveEnemies` of currently active enemies to bypass heavy native physics overlap checks in towers.
- **`Assets/Scripts/Towers/TowerBase.cs`**: We will replace the expensive `Physics.OverlapSphere` and `GetComponent` operations in `Update()` with a zero-allocation, high-performance distance-check loop on the `ActiveEnemies` list.
- **`Assets/Scripts/Core/GameStatisticsManager.cs`**: We will secure the file-saving path on mobile platforms to prevent folder creation and write permission exceptions in read-only folders.

---

# Implementation Steps

### Step 1: Secure JSON Logging Path for Mobile Devices
- **Description**: Update the log output directory path in `GameStatisticsManager.cs` to use `Application.persistentDataPath` when running on mobile devices (Android/iOS), while retaining project root storage for PC/macOS and Editor ease of access.
  - File to modify: `Assets/Scripts/Core/GameStatisticsManager.cs`
  - Modify `EndAndSaveSession()` to compute the `logsDir` as follows:
    ```csharp
    string logsDir;
    if (Application.isMobilePlatform)
    {
        logsDir = Path.Combine(Application.persistentDataPath, "Logs");
    }
    else
    {
        logsDir = Path.Combine(Application.dataPath, "../Logs");
    }
    ```
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Implement Static Active Enemies Tracking in EnemyHealth
- **Description**: Create a static list of currently alive and active enemies in `EnemyHealth.cs` to eliminate search overhead in towers.
  - File to modify: `Assets/Scripts/Enemies/EnemyHealth.cs`
  - Add public static list:
    ```csharp
    public static List<EnemyHealth> ActiveEnemies = new List<EnemyHealth>();
    ```
  - Add `OnEnable()` to register itself:
    ```csharp
    private void OnEnable()
    {
        if (!ActiveEnemies.Contains(this))
        {
            ActiveEnemies.Add(this);
        }
    }
    ```
  - Add `OnDisable()` to clean up:
    ```csharp
    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }
    ```
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 3: Optimize Tower Range Targeting (Remove Physics.OverlapSphere)
- **Description**: Re-engineer the tower's targeting logic to iterate over the direct references in `EnemyHealth.ActiveEnemies` using squared-distance checks (sqrMagnitude), reducing garbage allocations to **exactly 0**.
  - File to modify: `Assets/Scripts/Towers/TowerBase.cs`
  - Rewrite `UpdateTargetsInRange()`:
    ```csharp
    private void UpdateTargetsInRange()
    {
        targetsInRange.Clear();
        if (currentStats == null) return;

        float rangeSqr = currentStats.range * currentStats.range;
        Vector3 myPos = transform.position;

        var activeEnemies = EnemyHealth.ActiveEnemies;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyHealth enemy = activeEnemies[i];
            if (enemy != null && enemy.enabled && !enemy.IsDead)
            {
                float distSqr = (enemy.transform.position - myPos).sqrMagnitude;
                if (distSqr <= rangeSqr)
                {
                    targetsInRange.Add(enemy);
                }
            }
        }
    }
    ```
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

---

# Verification & Testing

### Test Case 1: Compilation Check
- Verify that editing `EnemyHealth.cs`, `TowerBase.cs`, and `GameStatisticsManager.cs` leaves the project without any compilation warnings or errors.

### Test Case 2: Statistics File Integrity on PC/Mobile Check
- Simulate a session and check that the JSON logs directory is generated correctly next to `Assets/` on PC/macOS Editor, and can fall back safely to `Application.persistentDataPath` on mobile profiles.

### Test Case 3: Combat Loop and Wave Spawning Behavior
- Run a wave and verify that towers perfectly target, rotate towards, and shoot enemies as before, without any change in gameplay behavior but with significantly improved CPU and GC frame-time performance.
