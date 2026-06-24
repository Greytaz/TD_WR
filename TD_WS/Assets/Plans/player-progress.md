# Project Overview
- Game Title: Tower Defense Sci-Fi
- High-Level Concept: A classic 3D grid-based tower defense game where players place towers (Archer, Cannon, Mage) to defend against waves of insectoid/shape enemies (Light, Heavy, Fast, Armored, and Bosses).
- Players: Single-player
- Inspiration / Reference Games: Desktop Tower Defense, Kingdom Rush
- Tone / Art Direction: Stylized Sci-Fi, colorful 3D primitive representations with particle effects and screen shakes
- Target Platform: StandaloneOSX / PC
- Screen Orientation / Resolution: Landscape 1920x1080
- Render Pipeline: Universal Render Pipeline (URP, PC_RPAsset)

# Game Mechanics
## Core Gameplay Loop
The player starts on the main menu, views their level/XP progress, and starts or continues a game. In-game, waves of enemies spawn and follow a path toward the base. The player earns gold by defeating enemies and clearing waves. They use this gold to build and upgrade towers on the grid. If enemies reach the base, player HP decreases. Surviving waves and defeating boss enemies awards experience points (XP) to the player upon game over, leading to level ups and tech tokens.

## Controls and Input Methods
- **Mouse Click**: Placements, upgrades, and HUD menu selections.
- **Escape Key**: Toggles game pause/unpause during play.

# UI
## Main Menu
The main menu panel (`MainMenuPanel`) will display the following player progress elements centered between the Best Wave text and the Start button:
- **Level**: `Level: X`
- **XP current / XP required**: `XP: X / Y`
- **Tech Tokens**: `Tech Tokens: Z`

These values will be updated dynamically at startup and whenever player progress changes.

# Key Asset & Context
## Existing Code to Modify
1. **`Assets/Scripts/Utils/EventBus.cs`**:
   - Add event `OnEnemyKilledData` passing `EnemyData` to track boss deaths.
   - Add event `OnPlayerProgressChanged` to notify UI of XP, level, or token changes.
   - Add trigger helpers.
2. **`Assets/Scripts/Enemies/EnemyHealth.cs`**:
   - Invoke `EventBus.OnEnemyKilledData` in the `Die()` method so boss deaths can be tracked.

## New Assets to Create
1. **`Assets/Scripts/Core/PlayerProgressManager.cs`**:
   - Singleton manager handling player Level, XP, and Tech Tokens.
   - Saves and loads progress using PlayerPrefs.
   - Tracks bosses killed in the current run and calculates XP on game over.
2. **`Assets/Scripts/UI/PlayerProgressUI.cs`**:
   - Component placed on the Main Menu panel that instantiates/finds and updates the Player Progress text elements.
3. **`Assets/Scripts/Editor/PlayerProgressSetup.cs`**:
   - Editor helper script to configure the scene (adds `PlayerProgressManager` and `PlayerProgressUI` to appropriate GameObjects) automatically and save.

# Implementation Steps

### Step 1: Event Bus & Enemy Health updates
- **Description**: Add necessary events to `EventBus.cs` and trigger them in `EnemyHealth.cs`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: Implement PlayerProgressManager
- **Description**: Create `PlayerProgressManager.cs` to manage saving, loading, resetting, and calculating run rewards. Implement required methods: `LoadProgress()`, `SaveProgress()`, `ResetProgress()`, `AddXP(int amount)`, `GetXPRequiredForNextLevel()`, and `GrantRunXP(int reachedWave, int killedBosses)`.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Implement PlayerProgressUI
- **Description**: Create `PlayerProgressUI.cs` to programmatically instantiate and style the progress text elements under `MainMenuPanel`, subscribing to `EventBus.OnPlayerProgressChanged`.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4: Scene Setup Editor Tool
- **Description**: Write `PlayerProgressSetup.cs` editor script to automatically attach `PlayerProgressManager` to the `[GameManager]` GameObject and `PlayerProgressUI` to the `[MainMenuPanel]` GameObject. Run the setup.
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

# Verification & Testing
## Automated/Manual Integration Tests
1. **Test Start State**: Clear PlayerPrefs, start game. Level = 1, XP = 0, Tokens = 0.
2. **XP & Level-up Test**:
   - Trigger `GrantRunXP(17, 3)` through a test command script or gameplay.
   - Verify that 320 XP is awarded (`17 * 10 + 3 * 50 = 320`).
   - Verify that the level increases from 1 to 3, and Tech Tokens increase from 0 to 2.
   - Verify the remaining XP is exactly 50 (`320 - 100 [Level 1] - 170 [Level 2] = 50`).
3. **Persistence Test**:
   - Quit the game/stop Play Mode.
   - Start the game/play again.
   - Verify that Level = 3, XP = 50, Tokens = 2 are loaded successfully from PlayerPrefs.
