# Rogue Trader Game API Reference

## Verified Source Links

All APIs documented here are from **ToyBox-RogueTrader** mod + **decompiled Code.dll**:
- **Repository**: https://github.com/xADDBx/ToyBox-RogueTrader
- **Decompiled**: `Code.dll` from `<GamePath>/Warhammer 40000 Rogue Trader_Data/Managed/`

---

> [!TIP]
> ## ✅ VERIFIED APIs (from decompiled Code.dll)
> 
| What | Verified Name | Source File |
|------|--------------|-------------|
| **Yellow AP** | `ActionPointsYellow` | `PartUnitCombatState.cs` |
| **Blue AP** | `ActionPointsBlue` | `PartUnitCombatState.cs` |
| **Max Blue AP** | `ActionPointsBlueMax` | `PartUnitCombatState.cs` |
| **AI Control** | `PartUnitBrain.IsAIEnabled` | `PartUnitBrain.cs` |
| **Pathfinding** | `PathfindingService.Instance.FindPathRT_Blocking` | `PathfindingService.cs` |
| **Move Params** | `UnitMoveToParams(ForcedPath, ...)` | `UnitMoveToParams.cs` |
| **Move Command** | `UnitMoveTo` | `UnitMoveTo.cs` |
| **Ability Command** | `UnitUseAbility(UnitUseAbilityParams)` | `UnitUseAbility.cs` |
| **Ability AP Cost** | `AbilityData.CalculateActionPointCost()` | `AbilityData.cs` |
| **Targeting** | `TargetWrapper` (implicit `Vector3`) | `TargetWrapper.cs` |
| **Ability Props** | `IsSpell`, `IsPsykerAbility`, `IsWeaponAbility` | `BlueprintAbility.cs` |

---

## Core Namespaces

```csharp
using Kingmaker;                              // Game.Instance
using Kingmaker.EntitySystem.Entities;        // BaseUnitEntity, UnitEntity
using Kingmaker.Mechanics.Entities;           // AbstractUnitEntity
using Kingmaker.UnitLogic;                    // PartHealth, Progression
using Kingmaker.UnitLogic.Parts;              // PartUnitCombatState, PartMovable
using Kingmaker.UnitLogic.Commands;           // UnitUseAbilityParams, UnitMoveTo
using Kingmaker.UnitLogic.Commands.Base;      // UnitCommand
using Kingmaker.UnitLogic.Abilities.Blueprints;  // Ability blueprints
using Kingmaker.Controllers.Combat;           // Combat controllers
using Kingmaker.Pathfinding;                  // ForcedPath
```

---

## Game Instance Access

```csharp
// Main game instance
Game.Instance

// Player data
Game.Instance.Player
Game.Instance.Player.MainCharacter.Entity    // Main character
Game.Instance.Player.Party                   // All party members
Game.Instance.Player.AllCharacters           // All characters including companions
Game.Instance.Player.PartyAndPets            // Party + pets

// Combat state
Game.Instance.Player.IsTurnBasedModeOn()

// Current area/state
Game.Instance.State.LoadedAreaState
Game.Instance.State.MapObjects
Game.Instance.State.AllUnits
```

---

## Unit Entity

```csharp
// Base class: BaseUnitEntity (extends AbstractUnitEntity, MechanicEntity)

// Checking unit type
unit.IsPlayerFaction      // Is in player's faction
unit.IsMainCharacter      // Is the main character
unit.IsEnemy()            // Is hostile
unit.IsPartyOrPet()       // Is party member or pet (extension method)
unit.IsStarship()         // Is a starship
unit.IsInGame             // Is currently in game

// Position
unit.Position             // Vector3 position
unit.CombatGroup.IsPlayerParty  // Is in player's party group

// Combat group
unit.CombatGroup

// Faction
unit.Faction.Set(factionBp)
```

---

## Combat State (PartUnitCombatState)

```csharp
// Access combat state
unit.CombatState

// Methods
unit.CombatState.LeaveCombat()
unit.CombatState.SpendActionPoints(int? yellow, float? blue)
unit.CombatState.SpendActionPointsAll()

// Check if in combat
unit.IsInCombat
```

---

## AI Control (Brain Switch) 🧠

> **Critical**: To control a unit via LLM, you must first disable its default AI!
> Otherwise the game's AI will act before your commands.

```csharp
// Get UI settings part (controls AI behavior toggle)
var uiSettings = unit.Parts.Get<PartUnitUISettings>();

// DISABLE AI - Let us control the unit
uiSettings.OverrideAIControlBehaviour = true;  // Override default
uiSettings.MakeCharacterAIControlled = false;  // WE control, not AI

// RE-ENABLE AI - Return control to game
uiSettings.OverrideAIControlBehaviour = false;

// Alternative: PartUnitBrain (if exists in assembly)
var brain = unit.Parts.Get<PartUnitBrain>();
brain.IsActive = false;  // Disable automatic decisions
```

### The Three Systems (per G3 Analysis)

| System | Class | Purpose |
|--------|-------|---------|
| **Brain Switch** | `PartUnitUISettings` | Disable default AI so unit waits for commands |
| **Action Queue** | `UnitCommand` | Execute move/attack/ability commands |
| **State Reader** | `BaseUnitDataUtils` | Read unit state for LLM prompts |

---

## Health & Stats

```csharp
// Health access
unit.Health.Damage                           // Current damage taken
PartHealth.RestUnit(unit)                    // Restore unit health

// Stats
unit.Stats.GetStat(StatType.HitPoints)
unit.Stats.GetStat(StatType.TemporaryHitPoints)

// Descriptor for more stat access
unit.Descriptor().Progression.Experience
unit.Descriptor().Progression.ExperienceTable
unit.Descriptor().Progression.m_CharacterLevel
```

---

## Abilities & Commands

```csharp
// Ability parameters
UnitUseAbilityParams instance
instance.Ability                             // The ability being used
instance.Ability.Caster                      // Who is casting
instance.Ability.Caster.IsInPlayerParty      // Is caster in party
instance.Ability.AbilityGroups               // Ability group list
instance.IgnoreCooldown                      // Skip cooldown check

// Unit commands
unit.Commands.Run(command)                   // Execute a command
unit.Commands.Queue                          // Command queue
```

---

## Movement

```csharp
// PartMovable
unit.Parts.Get<PartMovable>()
partMovable.ModifiedSpeedMps                 // Movement speed

// Move commands
UnitMoveTo moveTo = new UnitMoveTo(destination, 0.3f);
moveTo.MovementDelay = delay;
moveTo.Orientation = orientation;
moveTo.CreatedByPlayer = true;
moveTo.SpeedLimit = speedLimit;
moveTo.OverrideSpeed = speed;
unit.Commands.Run(moveTo);

// Move command creation
UnitHelper.CreateMoveCommandUnit()
UnitHelper.CreateMoveCommandParamsRT()
```

---

## Parts System

```csharp
// Getting parts from unit
unit.Parts.GetAll<T>()                       // Get all parts of type
unit.Parts.GetOptional<T>()                  // Get optional part
unit.Get<T>()                                // Get part directly

// Common parts
unit.Parts.Get<PartUnitCombatState>()
unit.Parts.Get<PartMovable>()
unit.Parts.Get<UnitPartCompanion>()
unit.Parts.GetOptional<PartVendor>()
unit.Parts.GetOptional<PartUnitUISettings>()
```

---

## Event System

```csharp
// Raise events
EventBus.RaiseEvent<IGroupChangerHandler>(h => h.HandleCall(...))

// Subscribe to events (in mod initialization)
EventBus.Subscribe(handler)
```

---

## Spawning Units

```csharp
// Spawn at position
Game.Instance.EntitySpawner.SpawnUnit(
    blueprintUnit,           // BlueprintUnit
    position,                // Vector3
    Quaternion.identity,     // Rotation
    Game.Instance.State.LoadedAreaState.MainState
);
```

---

## Selection

```csharp
// UI selection manager
UIAccess.SelectionManager
UIAccess.SelectionManager.SelectedUnits      // Currently selected units
UIAccess.SelectionManager.UpdateSelectedUnits()
```

---

## Blueprint Resources

```csharp
// Load blueprints
ResourcesLibrary.TryGetBlueprint<BlueprintArea>(guid)
ResourcesLibrary.TryGetBlueprint<BlueprintUnit>(guid)

// Root blueprints
Game.Instance.BlueprintRoot.Cheats.Enemy
Game.Instance.BlueprintRoot.PlayerFaction
```

---

## Game Modes

```csharp
// Current game mode
Game.Instance.CurrentMode
GameModeType.Default
GameModeType.Pause
GameModeType.GlobalMap
```

---

## Additional Notes

1. **Harmony patches** work on all these classes
2. **Extension methods** are used extensively (e.g., `IsPartyOrPet()`)
3. The game uses **Owlcat's entity-component-part system** for unit data
4. Most combat APIs are in `Kingmaker.UnitLogic.Parts` and `Kingmaker.Controllers.Combat`
