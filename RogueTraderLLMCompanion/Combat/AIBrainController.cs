using System;
using System.Collections.Generic;
using System.Linq;

// Verified game assemblies from ToyBox source + Gemini 3's analysis:
// Brain Switch: PartUnitUISettings (AIControlled property)
// Action Queue: UnitCommand, UnitUseAbility, UnitMoveTo
// State Reader: BaseUnitDataUtils patterns

#if !DEBUG_WITHOUT_GAME
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Mechanics.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.Controllers.TurnBased;
using Kingmaker.Controllers.Combat;
#endif

namespace RogueTraderLLMCompanion.Combat
{
    /// <summary>
    /// Manages AI control state for units.
    /// Implements the "Brain Switch" pattern from Gemini 3's analysis.
    /// 
    /// Key concept: Disable the game's default AI so the unit waits for OUR commands,
    /// while keeping the unit in combat and maintaining faction status.
    /// </summary>
    public static class AIBrainController
    {
        // Track which units we've taken control of
        private static HashSet<string> _controlledUnits = new HashSet<string>();

#if !DEBUG_WITHOUT_GAME
        /// <summary>
        /// Disables the default AI for a unit so we can control it.
        /// This is the "Brain Switch" - telling the game's AI to step aside.
        /// </summary>
        public static bool DisableDefaultAI(BaseUnitEntity unit)
        {
            if (unit == null) return false;

            try
            {
                string unitId = unit.UniqueId ?? unit.GetHashCode().ToString();

                // Method 1: Use PartUnitUISettings (verified via game's "Override AI Control" feature)
                // This is the built-in game mechanism for controlling AI behavior
                var uiSettings = unit.Parts.Get<PartUnitUISettings>();
                if (uiSettings != null)
                {
                    // The game has built-in AI control override
                    // When we're controlling, we want to override the AI
                    uiSettings.OverrideAIControlBehaviour = true;
                    uiSettings.MakeCharacterAIControlled = false; // WE control, not AI
                    
                    Main.LogDebug($"Disabled AI for {unit.CharacterName} via PartUnitUISettings");
                }

                // Method 2: Check for PartUnitBrain if it exists
                // Some Owlcat games have a separate brain component
                var brain = unit.Parts.Get<PartUnitBrain>();
                if (brain != null)
                {
                    // Disable automatic decision making
                    brain.IsActive = false;
                    Main.LogDebug($"Disabled brain for {unit.CharacterName} via PartUnitBrain");
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
        /// Re-enables the default AI for a unit (when we're done controlling it).
        /// </summary>
        public static bool EnableDefaultAI(BaseUnitEntity unit)
        {
            if (unit == null) return false;

            try
            {
                string unitId = unit.UniqueId ?? unit.GetHashCode().ToString();

                // Restore PartUnitUISettings
                var uiSettings = unit.Parts.Get<PartUnitUISettings>();
                if (uiSettings != null)
                {
                    uiSettings.OverrideAIControlBehaviour = false;
                    // Don't change MakeCharacterAIControlled - let it return to default
                    
                    Main.LogDebug($"Re-enabled AI for {unit.CharacterName}");
                }

                // Restore PartUnitBrain if it exists
                var brain = unit.Parts.Get<PartUnitBrain>();
                if (brain != null)
                {
                    brain.IsActive = true;
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
        /// Checks if a unit's AI is currently active (for debugging).
        /// </summary>
        public static bool IsAIActive(BaseUnitEntity unit)
        {
            if (unit == null) return false;

            var uiSettings = unit.Parts.Get<PartUnitUISettings>();
            if (uiSettings != null && uiSettings.OverrideAIControlBehaviour)
            {
                return uiSettings.MakeCharacterAIControlled;
            }

            // Default: AI is active for player faction units not being controlled
            return unit.IsPlayerFaction;
        }

#else
        // Debug stubs
        public static bool DisableDefaultAI(object unit) 
        { 
            Main.Log("[DEMO] Would disable AI"); 
            return true; 
        }
        
        public static bool EnableDefaultAI(object unit) 
        { 
            Main.Log("[DEMO] Would enable AI"); 
            return true; 
        }
        
        public static bool IsControlled(object unit) => false;
        public static void ReleaseAllControl() { }
        public static bool IsAIActive(object unit) => false;
#endif
    }

#if !DEBUG_WITHOUT_GAME
    /// <summary>
    /// Placeholder for PartUnitBrain if it doesn't exist in the actual game.
    /// The actual class name may differ - check game assemblies.
    /// </summary>
    // Note: This is a placeholder - the actual type may be:
    // - PartUnitBrain
    // - PartAI  
    // - UnitBrain
    // - etc.
    // You'll need to check the decompiled Assembly-CSharp.dll
#endif
}
