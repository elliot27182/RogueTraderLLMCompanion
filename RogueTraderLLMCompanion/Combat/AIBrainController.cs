using System;
using System.Collections.Generic;

// =============================================================================
// VERIFIED APIs from decompiled Code.dll:
// - PartUnitBrain.IsAIEnabled (get/set) - Controls AI behavior
// - PartUnitBrain is in Kingmaker.UnitLogic namespace
// =============================================================================

#if !DEBUG_WITHOUT_GAME
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
#endif

namespace RogueTraderLLMCompanion.Combat
{
    /// <summary>
    /// Manages AI control state for units.
    /// Implements the "Brain Switch" pattern - disabling default AI so LLM can control.
    /// 
    /// VERIFIED API from decompiled Code.dll (PartUnitBrain.cs line 139-149):
    /// - PartUnitBrain.IsAIEnabled { get; set; }
    /// </summary>
    public static class AIBrainController
    {
        // Track which units we've taken control of
        private static HashSet<string> _controlledUnits = new HashSet<string>();

#if !DEBUG_WITHOUT_GAME
        /// <summary>
        /// Disables the default AI for a unit so we can control it.
        /// Uses verified API: PartUnitBrain.IsAIEnabled
        /// </summary>
        public static bool DisableDefaultAI(BaseUnitEntity unit)
        {
            if (unit == null) return false;

            try
            {
                string unitId = unit.UniqueId ?? unit.GetHashCode().ToString();

                // VERIFIED API: PartUnitBrain.IsAIEnabled
                // From decompiled Code.dll/Kingmaker/UnitLogic/PartUnitBrain.cs
                var brain = unit.GetOptional<PartUnitBrain>();
                if (brain != null)
                {
                    brain.IsAIEnabled = false;
                    Main.Log($"Disabled AI for {unit.CharacterName} via PartUnitBrain.IsAIEnabled");
                }
                else
                {
                    Main.LogWarning($"No PartUnitBrain found for {unit.CharacterName}");
                }

                _controlledUnits.Add(unitId);
                return true;
            }
            catch (Exception ex)
            {
                Main.LogError($"Failed to disable AI for {unit?.CharacterName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Re-enables the default AI for a unit.
        /// </summary>
        public static bool EnableDefaultAI(BaseUnitEntity unit)
        {
            if (unit == null) return false;

            try
            {
                string unitId = unit.UniqueId ?? unit.GetHashCode().ToString();

                var brain = unit.GetOptional<PartUnitBrain>();
                if (brain != null)
                {
                    brain.IsAIEnabled = true;
                    Main.LogDebug($"Re-enabled AI for {unit.CharacterName}");
                }

                _controlledUnits.Remove(unitId);
                return true;
            }
            catch (Exception ex)
            {
                Main.LogError($"Failed to re-enable AI for {unit?.CharacterName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if we're currently controlling a unit.
        /// </summary>
        public static bool IsControlled(BaseUnitEntity unit)
        {
            if (unit == null) return false;
            string unitId = unit.UniqueId ?? unit.GetHashCode().ToString();
            return _controlledUnits.Contains(unitId);
        }

        /// <summary>
        /// Releases control of all units (cleanup).
        /// </summary>
        public static void ReleaseAllControl()
        {
            var allUnits = Game.Instance?.State?.AllUnits;
            if (allUnits != null)
            {
                foreach (var unit in allUnits)
                {
                    if (unit is BaseUnitEntity baseUnit && IsControlled(baseUnit))
                    {
                        EnableDefaultAI(baseUnit);
                    }
                }
            }
            _controlledUnits.Clear();
            Main.LogDebug("Released control of all units");
        }

        /// <summary>
        /// Checks if a unit's AI is currently active.
        /// </summary>
        public static bool IsAIActive(BaseUnitEntity unit)
        {
            if (unit == null) return false;

            var brain = unit.GetOptional<PartUnitBrain>();
            return brain?.IsAIEnabled ?? false;
        }

#else
        // Debug stubs
        public static bool DisableDefaultAI(object unit) 
        { 
            Main.Log("[DEMO] Would disable AI via PartUnitBrain.IsAIEnabled"); 
            return true; 
        }
        
        public static bool EnableDefaultAI(object unit) 
        { 
            Main.Log("[DEMO] Would enable AI via PartUnitBrain.IsAIEnabled"); 
            return true; 
        }
        
        public static bool IsControlled(object unit) => false;
        public static void ReleaseAllControl() { }
        public static bool IsAIActive(object unit) => false;
#endif
    }
}
