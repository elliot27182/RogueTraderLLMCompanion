using System;
using System.Reflection;
using HarmonyLib;

// Verified game assemblies from ToyBox source:
// https://github.com/xADDBx/ToyBox-RogueTrader/blob/main/ToyBox/Classes/MonkeyPatchin/BagOfPatches/Combat/ActionsRT.cs

#if !DEBUG_WITHOUT_GAME
using Kingmaker;
using Kingmaker.Controllers.TurnBased;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Parts;
#endif

namespace RogueTraderLLMCompanion.Combat
{
    /// <summary>
    /// Harmony patches to hook into the game's turn-based combat system.
    /// API sources: https://github.com/xADDBx/ToyBox-RogueTrader
    /// </summary>
    public static class TurnHooks
    {
        private static CombatController _combatController;

        /// <summary>
        /// Sets the combat controller reference for the hooks to use.
        /// </summary>
        public static void Initialize(CombatController controller)
        {
            _combatController = controller;
        }

#if !DEBUG_WITHOUT_GAME
        #region Turn Start Patch

        /// <summary>
        /// Patch for when a unit's turn begins.
        /// Targets TurnController.StartUnitTurn based on Owlcat game patterns.
        /// </summary>
        [HarmonyPatch(typeof(TurnController), "StartUnitTurn")]
        public static class TurnStartPatch
        {
            /// <summary>
            /// Prefix runs *before* the original method.
            /// Vital to disable AI before it ticks.
            /// </summary>
            static void Prefix(TurnController __instance)
            {
                try
                {
                    if (_combatController == null || !Main.Enabled)
                        return;

                    // Get the current unit from the turn controller
                    var currentUnit = __instance.CurrentUnit as BaseUnitEntity;
                    if (currentUnit != null && _combatController.ShouldControlUnit(currentUnit))
                    {
                        Main.LogDebug($"LLM Intercepting Turn: {currentUnit.CharacterName}");
                        _combatController.OnUnitTurnStart(currentUnit);
                    }
                }
                catch (Exception ex)
                {
                    Main.LogError($"Error in TurnStartPatch: {ex}");
                }
            }
        }

        #endregion

        #region Combat Start Patch

        /// <summary>
        /// Patch for when combat begins.
        /// </summary>
        [HarmonyPatch(typeof(TurnController), "StartCombat")]
        public static class CombatStartPatch
        {
            static void Postfix()
            {
                try
                {
                    if (_combatController != null && Main.Enabled)
                    {
                        _combatController.OnCombatStarted();
                        Main.Log("Combat started - LLM Companion active");
                    }
                }
                catch (Exception ex)
                {
                    Main.LogError($"Error in CombatStartPatch: {ex}");
                }
            }
        }

        #endregion

        #region Combat End Patch

        /// <summary>
        /// Patch for when combat ends.
        /// </summary>
        [HarmonyPatch(typeof(TurnController), "EndCombat")]
        public static class CombatEndPatch
        {
            static void Postfix()
            {
                try
                {
                    if (_combatController != null && Main.Enabled)
                    {
                        _combatController.OnCombatEnded();
                        Main.Log("Combat ended - LLM Companion deactivated");
                    }
                }
                catch (Exception ex)
                {
                    Main.LogError($"Error in CombatEndPatch: {ex}");
                }
            }
        }

        #endregion

        #region Turn End Patch

        /// <summary>
        /// Patch for when a unit's turn ends.
        /// </summary>
        [HarmonyPatch(typeof(TurnController), "EndCurrentUnitTurn")]
        public static class TurnEndPatch
        {
            static void Postfix(TurnController __instance)
            {
                try
                {
                    if (_combatController != null && Main.Enabled)
                    {
                        _combatController.OnUnitTurnEnd();
                    }
                }
                catch (Exception ex)
                {
                    Main.LogError($"Error in TurnEndPatch: {ex}");
                }
            }
        }

        #endregion

        #region Action Point Patch (Optional - for tracking)

        /// <summary>
        /// Patch PartUnitCombatState.SpendActionPoints to track AP usage.
        /// Verified from ToyBox ActionsRT.cs
        /// </summary>
        [HarmonyPatch(typeof(PartUnitCombatState), nameof(PartUnitCombatState.SpendActionPoints))]
        public static class ActionPointsPatch
        {
            static void Postfix(PartUnitCombatState __instance, int? yellow, float? blue)
            {
                try
                {
                    if (_combatController == null || !Main.Enabled)
                        return;

                    var owner = __instance.Owner as BaseUnitEntity;
                    if (owner != null && _combatController.ShouldControlUnit(owner))
                    {
                        Main.LogDebug($"AP spent - Yellow: {yellow}, Blue: {blue}");
                    }
                }
                catch (Exception ex)
                {
                    Main.LogError($"Error in ActionPointsPatch: {ex}");
                }
            }
        }

        #endregion

#else
        // No-op patches for debug builds without game
        public static class TurnStartPatch { }
        public static class CombatStartPatch { }
        public static class CombatEndPatch { }
        public static class TurnEndPatch { }
#endif
    }
}
