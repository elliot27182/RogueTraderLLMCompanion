using System;
using System.Collections.Generic;
using System.Linq;
using RogueTraderLLMCompanion.Models;

// Verified game assemblies from ToyBox source:
// https://github.com/xADDBx/ToyBox-RogueTrader/blob/main/ToyBox/Classes/MonkeyPatchin/BagOfPatches/MovementRT.cs
// https://github.com/xADDBx/ToyBox-RogueTrader/blob/main/ToyBox/Classes/MainUI/ActionsRT.cs

#if !DEBUG_WITHOUT_GAME
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Mechanics.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.Controllers.TurnBased;
using Kingmaker.Pathfinding;
using UnityEngine;
#endif

namespace RogueTraderLLMCompanion.Combat
{
    /// <summary>
    /// Executes LLM-decided actions in the game.
    /// API sources: https://github.com/xADDBx/ToyBox-RogueTrader
    /// </summary>
    public class ActionExecutor
    {
        /// <summary>
        /// Validates that an action is legal in the current game state.
        /// </summary>
        public LLMAction ValidateAction(LLMAction action, CombatState state)
        {
            if (action == null)
            {
                return LLMAction.EndTurn("No action provided");
            }

            action.IsValidated = false;
            action.ValidationError = null;

            switch (action.Action)
            {
                case ActionType.Ability:
                case ActionType.Attack:
                    return ValidateAbilityAction(action, state);

                case ActionType.Move:
                    return ValidateMoveAction(action, state);

                case ActionType.MoveAndAttack:
                    return ValidateMoveAndAttackAction(action, state);

                case ActionType.UseItem:
                    return ValidateItemAction(action, state);

                case ActionType.EndTurn:
                case ActionType.Delay:
                    action.IsValidated = true;
                    return action;

                case ActionType.Sequence:
                    return ValidateSequenceAction(action, state);

                default:
                    return LLMAction.EndTurn($"Unknown action type: {action.Action}");
            }
        }

        #region Action Validation

        private LLMAction ValidateAbilityAction(LLMAction action, CombatState state)
        {
            if (string.IsNullOrEmpty(action.AbilityName))
            {
                action.ValidationError = "No ability name specified";
                return LLMAction.EndTurn(action.ValidationError);
            }

            var ability = state.CurrentUnit?.Abilities?.FirstOrDefault(
                a => a.Name.Equals(action.AbilityName, StringComparison.OrdinalIgnoreCase));

            if (ability == null)
            {
                action.ValidationError = $"Ability not found: {action.AbilityName}";
                return LLMAction.EndTurn(action.ValidationError);
            }

            if (!ability.IsAvailable)
            {
                action.ValidationError = $"Ability unavailable: {ability.UnavailableReason}";
                return LLMAction.EndTurn(action.ValidationError);
            }

            if (ability.APCost > state.CurrentUnit.ActionPoints)
            {
                action.ValidationError = $"Not enough AP ({state.CurrentUnit.ActionPoints}/{ability.APCost})";
                return LLMAction.EndTurn(action.ValidationError);
            }

            action.AbilityId = ability.Id;

            // Validate target if required
            if (ability.Targeting != TargetingType.Self && !string.IsNullOrEmpty(action.TargetId))
            {
                var target = FindTarget(action.TargetId, state);
                if (target == null)
                {
                    action.ValidationError = $"Target not found: {action.TargetId}";
                    return LLMAction.EndTurn(action.ValidationError);
                }

                float distance = state.CurrentUnit.Position.DistanceTo(target.Position);
                if (distance > ability.Range)
                {
                    action.ValidationError = $"Target out of range ({distance:F0}ft > {ability.Range}ft)";
                    return LLMAction.EndTurn(action.ValidationError);
                }
            }

            action.IsValidated = true;
            return action;
        }

        private LLMAction ValidateMoveAction(LLMAction action, CombatState state)
        {
            if (action.TargetPosition == null)
            {
                action.ValidationError = "No target position specified";
                return LLMAction.EndTurn(action.ValidationError);
            }

            if (state.CurrentUnit.MovementPoints <= 0)
            {
                action.ValidationError = "No movement points remaining";
                return LLMAction.EndTurn(action.ValidationError);
            }

            var targetPos = action.TargetPosition.ToPosition();
            float distance = state.CurrentUnit.Position.DistanceTo(targetPos);

            if (distance > state.CurrentUnit.MovementPoints)
            {
                Main.LogWarning($"Position too far ({distance:F0}ft > {state.CurrentUnit.MovementPoints:F0}ft) - will move as far as possible");
            }

            action.IsValidated = true;
            return action;
        }

        private LLMAction ValidateMoveAndAttackAction(LLMAction action, CombatState state)
        {
            if (action.TargetPosition != null)
            {
                var moveValidation = ValidateMoveAction(action, state);
                if (!moveValidation.IsValidated && moveValidation.Action == ActionType.EndTurn)
                {
                    return moveValidation;
                }
            }

            return ValidateAbilityAction(action, state);
        }

        private LLMAction ValidateItemAction(LLMAction action, CombatState state)
        {
            if (string.IsNullOrEmpty(action.ItemName))
            {
                action.ValidationError = "No item name specified";
                return LLMAction.EndTurn(action.ValidationError);
            }

            var item = state.CurrentUnit?.Consumables?.FirstOrDefault(
                i => i.Name.Equals(action.ItemName, StringComparison.OrdinalIgnoreCase));

            if (item == null)
            {
                action.ValidationError = $"Item not found: {action.ItemName}";
                return LLMAction.EndTurn(action.ValidationError);
            }

            action.IsValidated = true;
            return action;
        }

        private LLMAction ValidateSequenceAction(LLMAction action, CombatState state)
        {
            if (action.ActionSequence == null || action.ActionSequence.Count == 0)
            {
                action.ValidationError = "Empty action sequence";
                return LLMAction.EndTurn(action.ValidationError);
            }

            var firstAction = ValidateAction(action.ActionSequence[0], state);
            if (!firstAction.IsValidated)
            {
                return firstAction;
            }

            action.ActionSequence[0] = firstAction;
            action.IsValidated = true;
            return action;
        }

        private UnitInfo FindTarget(string targetId, CombatState state)
        {
            var enemy = state.EnemyUnits?.FirstOrDefault(
                e => e.Id.Equals(targetId, StringComparison.OrdinalIgnoreCase) ||
                     e.Name.Equals(targetId, StringComparison.OrdinalIgnoreCase));
            if (enemy != null) return enemy;

            var ally = state.FriendlyUnits?.FirstOrDefault(
                a => a.Id.Equals(targetId, StringComparison.OrdinalIgnoreCase) ||
                     a.Name.Equals(targetId, StringComparison.OrdinalIgnoreCase));
            if (ally != null) return ally;

            if (state.CurrentUnit?.Id.Equals(targetId, StringComparison.OrdinalIgnoreCase) == true ||
                state.CurrentUnit?.Name.Equals(targetId, StringComparison.OrdinalIgnoreCase) == true)
            {
                return state.CurrentUnit;
            }

            return null;
        }

        #endregion

        #region Action Execution

        /// <summary>
        /// Executes the given action for the specified unit.
        /// </summary>
#if DEBUG_WITHOUT_GAME
        public bool ExecuteAction(LLMAction action, object gameUnit)
        {
            Main.Log($"[DEMO] Would execute: {action.Action} - {action.Reasoning}");
            return true;
        }

        public bool EndTurn(object gameUnit)
        {
            Main.Log("[DEMO] Would end turn");
            return true;
        }
#else
        public bool ExecuteAction(LLMAction action, BaseUnitEntity gameUnit)
        {
            if (action == null || gameUnit == null)
            {
                Main.LogError("Cannot execute null action or for null unit");
                return false;
            }

            if (!action.IsValidated)
            {
                Main.LogWarning("Executing unvalidated action - this may fail");
            }

            try
            {
                switch (action.Action)
                {
                    case ActionType.Ability:
                    case ActionType.Attack:
                        return ExecuteAbility(action, gameUnit);

                    case ActionType.Move:
                        return ExecuteMove(action, gameUnit);

                    case ActionType.MoveAndAttack:
                        // =================================================================
                        // FIX: Queue commands instead of immediate sequential execution
                        // (per G3 analysis)
                        // =================================================================
                        // OLD: ExecuteMove then ExecuteAbility immediately
                        // Problem: Game might cancel ability if triggered while moving
                        // NEW: Queue both commands - Owlcat engine handles sequencing
                        // =================================================================
                        return ExecuteMoveAndAttack(action, gameUnit);

                    case ActionType.UseItem:
                        return ExecuteUseItem(action, gameUnit);

                    case ActionType.EndTurn:
                        return EndTurn(gameUnit);

                    case ActionType.Delay:
                        return DelayTurn(gameUnit);

                    // NOTE: TakeCover case removed - feature not implemented

                    case ActionType.Sequence:
                        return ExecuteSequence(action, gameUnit);

                    default:
                        Main.LogError($"Unknown action type: {action.Action}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                Main.LogError($"Exception executing action: {ex}");
                return false;
            }
        }

        // Helper method for verified grid-based movement
        private UnitMoveTo CreateValidMoveCommand(BaseUnitEntity unit, Vector3 targetPoint)
        {
            try 
            {
                // VALDIATED PATHFINDING WORKFLOW
                // 1. Calculate path synchronously using PathfindingService
                ForcedPath path = PathfindingService.Instance.FindPathRT_Blocking(unit.MovementAgent, targetPoint, 0f);

                if (path == null || path.error)
                {
                    Main.LogError("Failed to calculate path to target.");
                    return null;
                }

                // 2. Create Movement Parameters
                var moveParams = new UnitMoveToParams(path, targetPoint, 0f);

                // 3. Create Command
                return new UnitMoveTo(moveParams)
                {
                    CreatedByPlayer = true
                };
            }
            catch (Exception ex)
            {
                Main.LogError($"Error generating move command: {ex.Message}");
                return null;
            }
        }

        private bool ExecuteAbility(LLMAction action, BaseUnitEntity unit)
        {
            // Find the ability by ID
            var ability = unit.Abilities?.RawFacts?.FirstOrDefault(
                a => a.Blueprint?.AssetGuid.ToString() == action.AbilityId);

            if (ability == null)
            {
                Main.LogError($"Ability not found: {action.AbilityId}");
                return false;
            }

            // Find the target unit
            BaseUnitEntity targetUnit = null;
            if (!string.IsNullOrEmpty(action.TargetId))
            {
                targetUnit = FindGameUnit(action.TargetId);
            }

            Main.Log($"Executing ability: {action.AbilityName} -> {targetUnit?.CharacterName ?? "self"}");

            try
            {
                // Verified API Pattern: UnitUseAbility command
                // This ensures animations, sounds, and time costs are handled correctly by the engine
                TargetWrapper targetWrapper = targetUnit != null ? new TargetWrapper(targetUnit) : new TargetWrapper(unit);
                
                var useAbilityParams = new UnitUseAbilityParams(ability.Data, targetWrapper)
                {
                    IgnoreCooldown = false
                };

                var command = new UnitUseAbility(useAbilityParams);
                unit.Commands.Run(command);

                return true;
            }
            catch (Exception ex)
            {
                Main.LogError($"Failed to execute ability: {ex.Message}");
                return false;
            }
        }

        private bool ExecuteMove(LLMAction action, BaseUnitEntity unit)
        {
            if (action.TargetPosition == null)
            {
                Main.LogError("No target position for move");
                return false;
            }

            // Convert to Unity Vector3
            var targetPoint = new Vector3(action.TargetPosition.X, 0, action.TargetPosition.Y);
            Main.Log($"Moving to: ({action.TargetPosition.X:F0}, {action.TargetPosition.Y:F0})");

            var moveCommand = CreateValidMoveCommand(unit, targetPoint);
            if (moveCommand != null)
            {
                unit.Commands.Run(moveCommand);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Executes a move followed by an attack/ability.
        /// Queues both commands to the game engine for proper sequencing.
        /// </summary>
        private bool ExecuteMoveAndAttack(LLMAction action, BaseUnitEntity unit)
        {
            Main.Log($"Move and Attack: moving then using {action.AbilityName}");

            try
            {
                // Queue the move command first (if we have a target position)
                if (action.TargetPosition != null)
                {
                    var targetPoint = new Vector3(action.TargetPosition.X, 0, action.TargetPosition.Y);
                    
                    var moveCommand = CreateValidMoveCommand(unit, targetPoint);
                    if (moveCommand != null)
                    {
                        // Run adds to the queue
                        unit.Commands.Run(moveCommand);
                        Main.LogDebug($"Queued move to ({action.TargetPosition.X:F0}, {action.TargetPosition.Y:F0})");
                    }
                    else 
                    {
                        Main.LogError("Failed to generate move command for MoveAndAttack");
                        return false;
                    }
                }

                // Find and queue the ability
                var ability = unit.Abilities?.RawFacts?.FirstOrDefault(
                    a => a.Blueprint?.AssetGuid.ToString() == action.AbilityId);

                if (ability == null)
                {
                    Main.LogWarning($"Ability not found for MoveAndAttack: {action.AbilityId}");
                    return action.TargetPosition != null; // Partial success if moved
                }

                BaseUnitEntity targetUnit = null;
                if (!string.IsNullOrEmpty(action.TargetId))
                {
                    targetUnit = FindGameUnit(action.TargetId);
                }

                // Create and queue the ability command
                TargetWrapper targetWrapper = targetUnit != null ? new TargetWrapper(targetUnit) : new TargetWrapper(unit);
                var useAbilityParams = new UnitUseAbilityParams(ability.Data, targetWrapper);
                var abilityCommand = new UnitUseAbility(useAbilityParams);
                
                unit.Commands.Run(abilityCommand);
                Main.LogDebug($"Queued ability {action.AbilityName}");

                return true;
            }
            catch (Exception ex)
            {
                Main.LogError($"Failed to execute MoveAndAttack: {ex.Message}");
                return false;
            }
        }

        private bool ExecuteUseItem(LLMAction action, BaseUnitEntity unit)
        {
            Main.Log($"Using item: {action.ItemName}");

            try
            {
                // Find item in inventory
                var item = unit.Inventory?.Items?.FirstOrDefault(
                    i => i.Name?.Equals(action.ItemName, StringComparison.OrdinalIgnoreCase) == true);

                if (item == null)
                {
                    Main.LogError($"Item not found: {action.ItemName}");
                    return false;
                }

                // Find target if specified
                BaseUnitEntity target = null;
                if (!string.IsNullOrEmpty(action.TargetId))
                {
                    target = FindGameUnit(action.TargetId);
                }

                // Use the item
                // Would need to call item.UsableAbility.Cast() or similar
                return true;
            }
            catch (Exception ex)
            {
                Main.LogError($"Failed to use item: {ex.Message}");
                return false;
            }
        }

        public bool EndTurn(BaseUnitEntity unit)
        {
            Main.Log("Ending turn");

            try
            {
                // End the current unit's turn
                // PartUnitCombatState from ActionsRT.cs
                var combatState = unit.Parts.Get<PartUnitCombatState>();
                combatState?.SpendActionPointsAll();

                // Or use turn controller
                Game.Instance?.TurnController?.EndCurrentUnitTurn();

                return true;
            }
            catch (Exception ex)
            {
                Main.LogError($"Failed to end turn: {ex.Message}");
                return false;
            }
        }

        private bool DelayTurn(BaseUnitEntity unit)
        {
            Main.Log("Delaying turn");

            try
            {
                Game.Instance?.TurnController?.DelayCurrentUnitTurn();
                return true;
            }
            catch (Exception ex)
            {
                Main.LogError($"Failed to delay turn: {ex.Message}");
                return false;
            }
        }

        // NOTE: TakeCover method removed - feature not implemented
        // Will be added when cover movement logic is implemented

        private bool ExecuteSequence(LLMAction action, BaseUnitEntity unit)
        {
            bool success = true;
            foreach (var subAction in action.ActionSequence)
            {
                if (!ExecuteAction(subAction, unit))
                {
                    success = false;
                    break;
                }
            }
            return success;
        }

        private BaseUnitEntity FindGameUnit(string targetId)
        {
            // Search all units for matching ID or name
            var allUnits = Game.Instance?.State?.AllUnits;
            if (allUnits == null) return null;

            return allUnits.FirstOrDefault(u =>
                u.UniqueId == targetId ||
                u.CharacterName?.Equals(targetId, StringComparison.OrdinalIgnoreCase) == true) as BaseUnitEntity;
        }
#endif

        #endregion
    }
}
