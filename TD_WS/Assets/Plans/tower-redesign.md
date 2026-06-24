# Project Overview
- Game Title: Tower Defense Sci-Fi
- High-Level Concept: Renaming and redesigning the core behavior and visual representations of the game's defensive towers.
- Players: Single-player
- Render Pipeline: Universal Render Pipeline (URP)

# Game Mechanics
## Tower Redesign
The game's three tower types are being renamed and visual and behavioral changes applied:
1. **Archer Tower** -> **Assault**: Left mechanically unchanged (direct damage).
2. **Cannon Tower** -> **Command**: Shell/bomb trajectory replaced by direct **Rocket** trajectory with straight-line flight, smoke/thrust trail, and same explosive splash damage.
3. **Mage Tower** -> **Energy**: Bullet-sphere projectile replaced by an instant **Laser Beam** connection that applies elemental control/debuff effects (stun, slow, burn) instantly with a fading visual line effect.

# UI
## Battle UI & Shop Panel
- All references to "Archer", "Cannon", and "Mage" will be replaced by "Assault", "Command", and "Energy" in labels, shop buttons, upgrade panel, tooltips, and perk choices.

# Key Asset & Context
## Existing Code to Modify
1. **`Assets/Scripts/Data/TowerData.cs`**:
   - Update `TowerType` enum from `Archer, Cannon, Mage` to `Assault, Command, Energy`.
2. **`Assets/Scripts/Towers/TowerBase.cs`**:
   - Update references of `TowerType.Archer` to `TowerType.Assault`.
   - Update references of `TowerType.Cannon` to `TowerType.Command`.
   - Update references of `TowerType.Mage` to `TowerType.Energy`.
3. **`Assets/Scripts/Data/TowerMetaSystem.cs`**:
   - Update unlocking logic defaults from `TowerType.Archer` to `TowerType.Assault`.
4. **`Assets/Scripts/Core/RunPerkManager.cs`**:
   - Update all perk options descriptions, titles, and stats modification rules to match "Assault", "Command", and "Energy".
5. **`Assets/Scripts/Enemies/EnemyHealth.cs`**:
   - Update comments for resistances.

## New Projectile Classes
1. **`Assets/Scripts/Projectiles/RocketProjectile.cs`**:
   - Rocket flying script with direct physical movement and splash damage.
2. **`Assets/Scripts/Projectiles/EnergyBeamProjectile.cs`**:
   - Laser beam projectile using `LineRenderer` that instantly hits target and applies control status effects.

# Implementation Steps

### Step 1: Update Tower Type Enum and Constants
- **Description**: Update `TowerType` enum inside `TowerData.cs` and fix references in `TowerMetaSystem.cs` and `TowerBase.cs`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: Implement Rocket and Laser Beam Projectiles
- **Description**: Create `RocketProjectile.cs` and `EnergyBeamProjectile.cs` implementing straight flight and instant fading beam mechanics.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Update RunPerkManager metadata
- **Description**: Update perk options to replace old tower names with Assault, Command, and Energy.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 4: Run Automation Setup Editor Tool
- **Description**: Create `TowerRenameSetup.cs` to programmatically update scriptable assets, scene buttons, and attach new projectile components to standard prefabs. Run the setup tool.
- **Assigned role**: developer
- **Dependencies**: Step 2, Step 3
- **Parallelizable**: No

# Verification & Testing
## Automated/Manual Integration Tests
1. Verify that all 3 tower assets in `Assets/Data/Towers/` have been renamed and contain updated string fields ("Assault", "Command", "Energy").
2. Build an **Energy** tower in-game and verify it shoots a fading purple laser beam instantly, applying slows/stuns.
3. Build a **Command** tower in-game and verify it fires a physical rocket with a smoke/thrust trail in a straight line.
4. Verify no old names appear on buttons, panels, or perks.
