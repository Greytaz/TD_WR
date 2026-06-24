# Project Overview
- Game Title: Tower Defense Sci-Fi
- High-Level Concept: A classic 3D grid-based tower defense game where players place towers to defend against waves of enemies.
- Players: Single-player
- Inspiration / Reference Games: Desktop Tower Defense, Kingdom Rush
- Tone / Art Direction: Stylized Sci-Fi, primitive 3D representations
- Target Platform: StandaloneOSX / PC
- Screen Orientation / Resolution: Landscape 1920x1080
- Render Pipeline: Universal Render Pipeline (URP, PC_RPAsset)

# Game Mechanics
## Core Gameplay Loop
The player places and upgrades towers on the grid to defend against insectoid waves. Completing waves grants gold, and defeating bosses awards Tech Tokens. In case the player closes the game or exits to the main menu mid-run, they should be able to continue from the exact wave they reached, recovering all their towers, stats, gold, lives, and active wave perks.

# UI
## Main Menu
- The **Continue** button will be active if and only if a valid active run save file exists on disk.
- Clicking **Continue** will restore the active run state (HP, Gold, Wave Index, Perks, and Towers) and load the game field.

# Key Asset & Context
## Existing Code to Modify
1. **`Assets/Scripts/Utils/SaveSystem.cs`**:
   - Add structures `ActiveRunSaveData` and `TowerSaveEntry`.
   - Add `SaveActiveRun(ActiveRunSaveData data)`, `LoadActiveRun()`, `HasActiveRun()`, and `ClearActiveRun()`.
2. **`Assets/Scripts/Core/GameManager.cs`**:
   - Hook into wave completion `HandleWaveCompleted(int waveIndex)` to trigger the autosave.
   - Update `ContinueGame()` to load the active run and restore all grid towers, HP, gold, and wave index.
   - Reset/clear active run on `GameOver()` or starting a `StartNewGame()`.
3. **`Assets/Scripts/Core/RunPerkManager.cs`**:
   - Add `LoadActiveRunPerks(List<string> perks)` to rebuild active perks state on continuation.
4. **`Assets/Scripts/UI/UIManager.cs`**:
   - Update `Start()` and `OnMainMenuClicked()` to set `continueButton.interactable` based on `SaveSystem.HasActiveRun()`.

# Implementation Steps

### Step 1: Update SaveSystem structure and serialization
- **Description**: Add `ActiveRunSaveData` serialization class and autosave read/write methods to `SaveSystem.cs`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: Implement Save/Load support in RunPerkManager
- **Description**: Add `LoadActiveRunPerks` in `RunPerkManager.cs` to restore the list of active perks.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Integrate autosave trigger & loading in GameManager
- **Description**: Update `GameManager.cs` to write state on wave completion and load state (including rebuilding the grid of towers with their respective upgraded parts) in `ContinueGame()`.
- **Assigned role**: developer
- **Dependencies**: Step 2
- **Parallelizable**: No

### Step 4: Link Continue button check in UIManager
- **Description**: Bind the main menu `Continue` button's interactability to `SaveSystem.HasActiveRun()` in `UIManager.cs`.
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

# Verification & Testing
## Play Mode Integration Test
1. Clear existing saves and start a new game.
2. Build an Archer tower and upgrade its Weapon part to Level 2.
3. Complete wave 1.
4. Exit to the Main Menu (which will save progress).
5. Verify that the **Continue** button is interactable.
6. Click **Continue** and verify:
   - Gold and HP are restored exactly as they were.
   - The Archer tower is spawned back on the grid with its Weapon part upgraded to Level 2.
   - The wave index is restored to wave 2.
