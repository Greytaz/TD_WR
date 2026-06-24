# Project Overview 
- **Game Title**: Modular Tower Defense
- **High-Level Concept**: A strategic tower defense game where players build, upgrade, and customize defense towers using modular parts (bases, bodies, weapons) to survive waves of beetle enemies along a path.
- **Players**: Single player
- **Inspiration / Reference Games**: Classic Tower Defense, Modular TD
- **Tone / Art Direction**: Vibrant 3D modular look
- **Target Platform**: Standalone macOS / OSX
- **Screen Orientation / Resolution**: Landscape (1920x1080)
- **Render Pipeline**: Universal Render Pipeline (URP)

# Game Mechanics 
## Core Gameplay Loop
The player places modular towers on a grid to intercept waves of enemies marching along a road. Surviving enemies reach the base and deduct player lives. The game continues through escalating waves until the player's lives reach 0, leading to a Game Over.

## Controls and Input Methods
Point-and-click mouse controls for building/upgrading towers, interacting with UI buttons, and hotkeys like Escape to pause the game. Uses the New Input System.

# UI
Not applicable: Screen outputs, UI, and hotkeys for statistics are explicitly not required. The stats collection system will run entirely in the background, outputting data directly to a JSON file after game completion.

# Key Asset & Context
- **`Assets/Scripts/Utils/EventBus.cs`**: Contains the central event broker system. We will expand it with a tower damage event.
- **`Assets/Scripts/Enemies/EnemyHealth.cs`**: Handles damage calculation and resistances. We will update `TakeDamage` to take the damage source (tower) and trigger damage tracking.
- **`Assets/Scripts/Projectiles/ProjectileBase.cs`** (and subclasses: `HomingProjectile.cs`, `EnergyBeamProjectile.cs`, `RocketProjectile.cs`, `SplashProjectile.cs`): Handles projectile collision and hit logic. We will update them to hold and pass the reference of the tower that shot them.
- **`Assets/Scripts/Core/GameStatisticsManager.cs`** *(New File)*: Automatically manages statistics tracking, compiles session logs, handles file system directories, and writes JSON outputs to the `Logs/` folder.

---

# Implementation Steps

### Step 1: Add Tower Damage Event to EventBus
- **Description**: Add the `OnTowerDamageDealt` event to `EventBus.cs` so any system can listen to damage events.
  - File to modify: `Assets/Scripts/Utils/EventBus.cs`
  - Add:
    ```csharp
    public static Action<Towers.TowerBase, float> OnTowerDamageDealt;
    public static void TriggerTowerDamageDealt(Towers.TowerBase tower, float damage) => OnTowerDamageDealt?.Invoke(tower, damage);
    ```
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Track Source Tower in Projectiles
- **Description**: Update `ProjectileBase` to reference the spawning tower.
  - File to modify: `Assets/Scripts/Projectiles/ProjectileBase.cs`
  - Add a protected field: `protected Towers.TowerBase sourceTower;`
  - Add a public method: `public void SetSourceTower(Towers.TowerBase source) => sourceTower = source;`
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 3: Assign Source Tower in Tower Shooting
- **Description**: Update `TowerBase.cs` to assign itself as the source tower of any projectile it spawns.
  - File to modify: `Assets/Scripts/Towers/TowerBase.cs`
  - Modify `Shoot()` to call `projectile.SetSourceTower(this);` after spawning the projectile.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4: Propagate Tower Damage Source to EnemyHealth
- **Description**: Pass the spawning tower from projectiles to `EnemyHealth.TakeDamage`, calculate exact damage taken, and trigger the EventBus.
  - File to modify: `Assets/Scripts/Enemies/EnemyHealth.cs`
    - Update signature: `public void TakeDamage(float damage, DamageType type, bool isCritical = false, Towers.TowerBase source = null)`
    - Inside `TakeDamage`, calculate exact damage deducted from current health: `float actualDamage = Mathf.Min(finalDamage, currentHealth);`
    - Trigger event: `if (source != null && actualDamage > 0f) EventBus.TriggerTowerDamageDealt(source, actualDamage);`
  - Files to modify: `ProjectileBase.cs`, `HomingProjectile.cs`, `EnergyBeamProjectile.cs`, `RocketProjectile.cs`, `SplashProjectile.cs`
    - Modify all `target.TakeDamage(...)` and `enemy.TakeDamage(...)` calls to pass the `sourceTower` parameter.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 2
- **Parallelizable**: No

### Step 5: Implement GameStatisticsManager
- **Description**: Create a persistent, self-initializing manager that subscribes to gameplay events, tracks run metrics, and saves them to pretty JSON in the `Logs` folder.
  - File to create: `Assets/Scripts/Core/GameStatisticsManager.cs`
  - Features:
    - Uses `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]` to auto-instantiate, requiring zero manual scene setup.
    - Listens to: `OnWaveStarted`, `OnWaveCompleted`, `OnEnemyKilledData`, `OnEnemyReachedBaseData`, `OnTowerDamageDealt`, `OnGameOver`, `OnGameRestarted`.
    - Tracks killed and passed enemies by their name/type, total tower damage by type, and individual tower performance (using grid position via `GridManager.Instance.WorldToGrid`).
    - Detects run start and run end (via game over, quit, or restart) and writes pretty-printed JSON to `../Logs/game_stats_YYYYMMDD_HHmmss.json`.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 4
- **Parallelizable**: No

---

# Verification & Testing

### Test Case 1: Logs Directory Creation
- Run the game and trigger a Game Over. Verify that a folder named `Logs` is automatically created at the project root directory (next to the `Assets` folder).

### Test Case 2: Statistics File Integrity & Content
- Complete a couple of waves, build different types of towers (Assault, Energy, Command), upgrade some parts, allow some enemies to die, and let others pass.
- Trigger Game Over and inspect the generated JSON file in `Logs/`. Verify:
  - **Timestamp**: Valid date and time.
  - **Waves Survived**: Matches the wave index reached.
  - **Killed/Passed mobs**: Exact name and count matching your play session.
  - **Tower Damage**: Matches damage dealt by each type and lists coordinates/tiers/individual damage for each placed tower on the grid.

### Test Case 3: Clean Sessions on Restart/Exit
- Play a partial game, then click Restart or return to Main Menu mid-game. Verify that a JSON log is successfully written for the completed session, and a new, clean session is initialized for the next game.
