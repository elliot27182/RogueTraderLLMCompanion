# Antigravity Verification & Fixes Report (2025-12-05)

This log documents the changes made to the `RogueTraderLLMCompanion` mod after verifying the code against the actual decompiled game assemblies (`Code.dll`).

## 1. Critical Fix: Movement Logic (`ActionExecutor.cs`)

### The Issue
The mod initially attempted to move units using a simple constructor: `new UnitMoveTo(targetPosition, threshold)`. Validation against the game code revealed this constructor does not exist or does not function as expected for grid-based movement in combat.

### The Verified Game API
Decompiling `Code.dll` (`Kingmaker.UnitLogic.Commands.UnitMoveTo`, `Kingmaker.Pathfinding.PathfindingService`) revealed:
1.  **Grid Movement**: Requires a `ForcedPath` object to navigate the tile grid properly.
2.  **Pathfinding Service**: The game uses `PathfindingService.Instance.FindPathRT_Blocking` for calculating paths in real-time/combat contexts.
3.  **Command Construction**: The `UnitMoveTo` command requires `UnitMoveToParams`, which in turn wraps the calculated `ForcedPath`.

### The Fix
I updated `ExecuteMove` in `ActionExecutor.cs` to implement the correct workflow:

```csharp
// 1. Calculate Path (Synchronous)
ForcedPath path = PathfindingService.Instance.FindPathRT_Blocking(unit.MovementAgent, targetPoint, 0f);

// 2. Create Params
var moveParams = new UnitMoveToParams(path, targetPoint, 0f);

// 3. Create Command
var moveCommand = new UnitMoveTo(moveParams)
{
    CreatedByPlayer = true
};

// 4. Run
unit.Commands.Run(moveCommand);
```

## 2. Verified Correct Implementations

I analyzed other core components and confirmed they match the game's API:

*   **AI Control (`AIBrainController.cs`)**:
    *   **Verified**: `PartUnitBrain.IsAIEnabled` (bool property) is the correct way to toggle the default AI. The mod's "Brain Switch" logic is correct.
*   **Action Points (`CombatStateExtractor.cs`)**:
    *   **Verified**: `PartUnitCombatState` properties `ActionPointsYellow` (int) and `ActionPointsBlue` (float) are the correct APIs for reading AP/MP.

## 3. Documentation

I have updated `GAME_API_REFERENCE.md` in the project root with the fully verified method signatures to prevent future hallucinations.

---
**Summary for Claude**:
The "Hallucination Check" passed for AI Control and State Reading, but failed for `UnitMoveTo`. The movement logic was rewritten to use the `PathfindingService` -> `ForcedPath` -> `UnitMoveToParams` pipeline found in `Kingmaker.UnitLogic.Commands`.
