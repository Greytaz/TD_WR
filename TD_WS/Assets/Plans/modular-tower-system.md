# Project Overview
- **Game Title**: Bug Tower Defense (Modular Edition)
- **High-Level Concept**: A grid-based 3D Tower Defense game where players build, upgrade, and customize modular towers (comprising of Base, Body, and Turret components) to defend against waves of insect enemies.
- **Players**: Single player (VS AI waves of enemies)
- **Inspiration / Reference Games**: Warcraft 3 TD, Kingdom Rush, modular Tower Defense concepts
- **Tone / Art Direction**: Low-poly proto-stylized 3D using basic geometric primitives (Cylinders, Cubes) and flat-shaded materials
- **Target Platform**: PC / Standalone OS X
- **Screen Orientation / Resolution**: Landscape (1920x1080)
- **Render Pipeline**: Universal Render Pipeline (URP, PC_RPAsset)

# Game Mechanics
## Core Gameplay Loop
1. **Preparation**: Player selects and places towers on buildable grid cells using Gold.
2. **Modular Upgrades**: Player selects a placed tower and upgrades its individual parts (Base, Body, Turret) to Tier 3. Each part provides unique stat bonuses (buffs).
3. **Defend**: Enemies with various Armor Types (Light, Heavy, Magical) spawn in waves and walk along the path waypoints.
4. **Combat**: Towers shoot projectiles, dealing damage scaled by the combined buffs of their modular parts and target armor type multipliers.
5. **Reward**: Defeating enemies rewards Gold, allowing further placement and upgrades.

## Controls and Input Methods
- **Mouse / Touch click**: Selecting grid cells to place towers, and clicking placed towers to open the upgrade panel.
- **On-Screen Buttons (UI)**: Buying towers and upgrading individual parts (Base, Body, Weapon).

# UI
### Upgrade Panel Wireframe & Modifications
The current `UpgradePanel` contains a single Upgrade button. We will modify it to support individual upgrades for **Base**, **Body**, and **Weapon**:
```
+-------------------------------------------------------------+
| Archer Tower (Active Stats: Dmg 15, Range 6, Spd 1.5/s)      |
+-------------------------------------------------------------+
|  [ BASE ] Tier 1 -> Tier 2 (Cost: 50G)                     |
|  Buffs: +2 Range, +10% Crit                                |
|  [ UPGRADE BASE ]                                           |
+-------------------------------------------------------------+
|  [ BODY ] Tier 1 -> Tier 2 (Cost: 60G)                     |
|  Buffs: +1.15x Fire Rate                                   |
|  [ UPGRADE BODY ]                                           |
+-------------------------------------------------------------+
|  [ WEAPON ] Tier 2 -> Tier 3 (Cost: MAX)                    |
|  Buffs: +20% Dmg vs Heavy Armor, +15% Crit                 |
|  [ MAX TIER ]                                              |
+-------------------------------------------------------------+
|  [ SELL FOR 70G ]                [ PRIORITY: FIRST ]        |
+-------------------------------------------------------------+
```

# Key Asset & Context
### 1. Armor Type Enum & Config
We will define armor types and configure existing enemy structures.
File: `Assets/Scripts/Data/EnemyData.cs`
- Add `public enum ArmorType { Light, Heavy, Magical }`
- Add `public ArmorType armorType;` to `EnemyData` class.

### 2. Tower Part Component
File: `Assets/Scripts/Towers/TowerPart.cs` (New)
- Holds visual prefab for the part, the part's tier, and its unique buffs (Bonus Damage, Crit Chance, Attack Speed Multiplier, Armor multipliers).

### 3. Modular Tower Assembly & Live Stats Calculation
File: `Assets/Scripts/Towers/TowerBase.cs`
- Store current tiers of the three parts: `baseTier`, `bodyTier`, `weaponTier` (each 1-3).
- Hold references to part prefabs (3 tiers each for Base, Body, Weapon).
- Calculate combined/live stats:
  - Final damage: Base tier stats + part bonuses.
  - Final crit chance: Base tier stats + part bonuses.
  - Final attack speed (fire rate): Base tier stats * speed multipliers.
  - Final armor damage multipliers.
- Update `RebuildVisuals()` to instantiate the modular parts:
  - Base: Cube (parallelepiped) with 90% of grid cell size width and small height.
  - Body: Unchanged cylinder visual.
  - Weapon: Unchanged head visual attached to the rotatable pivot.

# Implementation Steps

### Step 1: Implement Armor Types & Enemy Config
- **Description**: Define the `ArmorType` enum and add it to `EnemyData`. Update existing enemy assets (`FastEnemy`, `HeavyEnemy`, `ArmoredEnemy`, `LightEnemy`, `BossEnemy`) to assign their corresponding armor type in the Inspector. Modify `EnemyHealth.TakeDamage` to take armor-based modifiers into account.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Step 2: Implement Modular Tower Parts Structure & Live Stats compilation
- **Description**: Create `TowerPart.cs` component. Extend `TowerBase.cs` and `TowerData.cs` to hold and reference arrays of `TowerPart` prefabs for each tier (Base, Body, Weapon). Implement property calculations in `TowerBase` that compile the combined live stats (damage, fire rate, crit, armor multipliers) from the equipped parts.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Implement Visual Assembly in TowerBase
- **Description**: Update `TowerBase.RebuildVisuals()` to dynamically instantiate and assemble the selected modular parts.
  - **Base visual**: Spawn a parallelepiped (width: 90% of `cellSize` = `1.35f`, height: `0.15f`) at the bottom.
  - **Body visual**: Spawn the body cylinder and place it on top of the base.
  - **Weapon visual**: Spawn the weapon head on top of the body and attach it to the rotating pivot.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4: Create Part Prefabs & Assign in TowerData Assets
- **Description**: Create modular part prefabs for Archer, Cannon, and Mage towers for Tiers 1-3. Assign these prefabs in the corresponding `TowerData` scriptable objects in the project.
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: Yes

### Step 5: Update Projectiles to Apply Modular Stats
- **Description**: Update `ProjectileBase` and its subclasses (`HomingProjectile`, `SplashProjectile`) to receive compiled live stats from the tower (including custom damage against Light, Heavy, and Magical armor) when fired, and apply the correct damage multipliers against the hit target's armor.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 6: Create Three-Part Upgrade UI
- **Description**: Redesign `UpgradePanel.cs` and its corresponding UI prefab. Add 3 distinct sections/buttons for upgrading the Base, Body, and Weapon respectively. Wire up the buttons to a modified `TowerUpgrade.cs` to allow upgrading individual parts using Gold.
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

# Verification & Testing
### Manual Validation Checks:
1. **Modular Assembly Check**: Place an Archer, Cannon, and Mage tower. Verify that they are assembled visually with the new parallelepiped base (90% width of the cell) and correct body and weapon.
2. **Individual Upgrades Check**: Select a tower. Click Upgrade Base, Upgrade Body, and Upgrade Weapon. Verify that:
   - Gold is deducted correctly.
   - The corresponding part is updated visually (e.g. base changes or upgrades).
   - Live tower stats (Damage, Speed, Range, Crit) are compiled and shown correctly in the Upgrade UI.
3. **Armor Multipliers Check**:
   - Spawn a Light Armor enemy and a Heavy Armor enemy.
   - Fire at them with a tower that has bonus damage against Heavy Armor. Verify via damage floating text that the Heavy Armor enemy takes more damage compared to the base damage.
   - Check that crit calculation uses the combined crit chance and displays `CRIT! [Damage]` in crimson text.
4. **Console Logs Cleanliness**: Ensure no null references or missing references are thrown during placement, upgrading, or shooting.
