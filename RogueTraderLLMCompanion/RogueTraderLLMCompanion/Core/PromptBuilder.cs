using System;
using System.Text;
using RogueTraderLLMCompanion.Models;

namespace RogueTraderLLMCompanion.Core
{
    /// <summary>
    /// Builds structured prompts from combat state for LLM processing.
    /// </summary>
    public static class PromptBuilder
    {
        /// <summary>
        /// Builds a complete combat prompt from the current state.
        /// </summary>
        public static string BuildCombatPrompt(CombatState state, ModSettings settings)
        {
            var sb = new StringBuilder();

            // Combat overview
            sb.AppendLine("=== COMBAT SITUATION ===");
            sb.AppendLine($"Round: {state.RoundNumber}");
            if (state.Environment != null)
            {
                sb.AppendLine($"Location: {state.Environment.AreaName ?? "Unknown"}");
                if (state.Environment.IsVoidCombat)
                    sb.AppendLine("Environment: Void Ship Combat");
            }
            sb.AppendLine($"Party Momentum: {state.Momentum}");
            if (state.DesperateMeasure > 0)
                sb.AppendLine($"Desperate Measure Points: {state.DesperateMeasure}");
            sb.AppendLine();

            // Current unit (whose turn it is)
            if (state.CurrentUnit != null)
            {
                sb.AppendLine("=== CURRENT UNIT (YOUR TURN) ===");
                BuildUnitSection(sb, state.CurrentUnit, detailed: true);
                sb.AppendLine();

                // Available abilities
                sb.AppendLine("AVAILABLE ABILITIES:");
                if (state.CurrentUnit.Abilities != null && state.CurrentUnit.Abilities.Count > 0)
                {
                    foreach (var ability in state.CurrentUnit.Abilities)
                    {
                        if (ability.IsAvailable)
                        {
                            sb.AppendLine(ability.ToSummary());
                        }
                    }
                    
                    // Also list unavailable abilities with reasons
                    sb.AppendLine("\nUnavailable (for reference):");
                    foreach (var ability in state.CurrentUnit.Abilities)
                    {
                        if (!ability.IsAvailable)
                        {
                            sb.AppendLine($"  - {ability.Name}: {ability.UnavailableReason}");
                        }
                    }
                }
                else
                {
                    sb.AppendLine("  None available");
                }
                sb.AppendLine();

                // Equipment
                if (state.CurrentUnit.Weapons?.Count > 0)
                {
                    sb.AppendLine("EQUIPPED WEAPONS:");
                    foreach (var weapon in state.CurrentUnit.Weapons)
                    {
                        string traits = weapon.Traits?.Count > 0 ? $" [{string.Join(", ", weapon.Traits)}]" : "";
                        sb.AppendLine($"  - {weapon.Name} ({weapon.Type}): {weapon.Damage}, Range: {weapon.Range}ft, AP: {weapon.APCost}{traits}");
                    }
                    sb.AppendLine();
                }

                // Consumables
                if (settings.UseConsumables && state.CurrentUnit.Consumables?.Count > 0)
                {
                    sb.AppendLine("CONSUMABLE ITEMS:");
                    foreach (var item in state.CurrentUnit.Consumables)
                    {
                        sb.AppendLine($"  - {item.Name} x{item.Quantity}: {item.Effect} (AP: {item.APCost})");
                    }
                    sb.AppendLine();
                }
            }

            // Friendly units
            if (state.FriendlyUnits?.Count > 0)
            {
                sb.AppendLine("=== ALLIED UNITS ===");
                foreach (var unit in state.FriendlyUnits)
                {
                    if (unit.Id != state.CurrentUnit?.Id)
                    {
                        BuildUnitSection(sb, unit, detailed: false);
                    }
                }
                sb.AppendLine();
            }

            // Enemy units
            if (state.EnemyUnits?.Count > 0)
            {
                sb.AppendLine("=== ENEMY UNITS ===");
                foreach (var enemy in state.EnemyUnits)
                {
                    BuildEnemySection(sb, enemy, state.CurrentUnit);
                }
                sb.AppendLine();
            }

            // Turn order
            if (state.TurnOrder?.Count > 0)
            {
                sb.AppendLine("=== TURN ORDER (Remaining) ===");
                sb.AppendLine(string.Join(" -> ", state.TurnOrder));
                sb.AppendLine();
            }

            // Nearby cover (if available)
            if (state.Environment?.NearbyCover?.Count > 0)
            {
                sb.AppendLine("=== NEARBY COVER POSITIONS ===");
                foreach (var cover in state.Environment.NearbyCover)
                {
                    sb.AppendLine($"  - {cover.Type} cover at {cover.Position} ({cover.Distance:F0}ft away)");
                }
                sb.AppendLine();
            }

            // Combat style guidance based on settings
            sb.AppendLine("=== TACTICAL GUIDANCE ===");
            AddTacticalGuidance(sb, settings, state);
            sb.AppendLine();

            // Request for action
            sb.AppendLine("=== YOUR ACTION ===");
            sb.AppendLine("Analyze the situation and choose the best action. Respond with a JSON object.");

            return sb.ToString();
        }

        private static void BuildUnitSection(StringBuilder sb, UnitInfo unit, bool detailed)
        {
            sb.AppendLine($"[{unit.Id}] {unit.Name} ({unit.UnitType ?? unit.Archetype})");
            sb.AppendLine($"  HP: {unit.CurrentHP}/{unit.MaxHP} ({unit.HealthPercent}%)");
            sb.AppendLine($"  AP: {unit.ActionPoints}/{unit.MaxActionPoints}, MP: {unit.MovementPoints:F0}/{unit.MaxMovementPoints:F0}ft");
            sb.AppendLine($"  Position: {unit.Position}, Cover: {unit.Cover}");

            if (detailed)
            {
                sb.AppendLine($"  WS: {unit.WeaponSkill}, BS: {unit.BallisticSkill}");
                sb.AppendLine($"  Armor: {unit.Armor}, Deflection: {unit.Deflection}, Dodge: {unit.DodgeChance}%");
            }

            if (unit.StatusEffects?.Count > 0)
            {
                sb.Append("  Status: ");
                foreach (var effect in unit.StatusEffects)
                {
                    string debuff = effect.IsDebuff ? "!" : "";
                    sb.Append($"[{debuff}{effect.Name}:{effect.RemainingRounds}r] ");
                }
                sb.AppendLine();
            }

            if (unit.Wounds > 0)
            {
                sb.AppendLine($"  WOUNDS: {unit.Wounds} (critical injuries)");
            }
        }

        private static void BuildEnemySection(StringBuilder sb, UnitInfo enemy, UnitInfo currentUnit)
        {
            float distance = currentUnit?.Position != null && enemy.Position != null
                ? currentUnit.Position.DistanceTo(enemy.Position)
                : 0;

            string threatLevel = GetThreatLevelString(enemy.ThreatLevel);
            string psykerTag = enemy.IsPsyker ? " [PSYKER]" : "";
            string rangedTag = enemy.HasRangedAttack ? " [RANGED]" : "";

            sb.AppendLine($"[{enemy.Id}] {enemy.Name} ({enemy.UnitType}){psykerTag}{rangedTag}");
            sb.AppendLine($"  HP: {enemy.CurrentHP}/{enemy.MaxHP} ({enemy.HealthPercent}%) - Threat: {threatLevel}");
            sb.AppendLine($"  Position: {enemy.Position}, Distance: {distance:F0}ft, Cover: {enemy.Cover}");
            sb.AppendLine($"  Armor: {enemy.Armor}, Dodge: {enemy.DodgeChance}%");

            if (enemy.StatusEffects?.Count > 0)
            {
                sb.Append("  Debuffs: ");
                foreach (var effect in enemy.StatusEffects)
                {
                    if (effect.IsDebuff)
                    {
                        sb.Append($"[{effect.Name}:{effect.RemainingRounds}r] ");
                    }
                }
                sb.AppendLine();
            }
        }

        private static string GetThreatLevelString(int threatLevel)
        {
            if (threatLevel >= 80) return "CRITICAL";
            if (threatLevel >= 60) return "HIGH";
            if (threatLevel >= 40) return "MEDIUM";
            if (threatLevel >= 20) return "LOW";
            return "MINIMAL";
        }

        private static void AddTacticalGuidance(StringBuilder sb, ModSettings settings, CombatState state)
        {
            // Note: AI decides priorities autonomously based on battlefield state
            sb.AppendLine("You are an expert tactical AI. Analyze the battlefield and decide the optimal action.");
            sb.AppendLine("Consider all factors: enemy positions, threat levels, ally status, cover, abilities, and resources.");
            sb.AppendLine();

            // Combat style is just a hint, not a strict rule
            switch (settings.PreferredCombatStyle)
            {
                case CombatStyle.Aggressive:
                    sb.AppendLine("- STYLE HINT: Player prefers aggressive tactics when viable");
                    break;

                case CombatStyle.Defensive:
                    sb.AppendLine("- STYLE HINT: Player prefers cautious, defensive play");
                    break;

                case CombatStyle.Support:
                    sb.AppendLine("- STYLE HINT: Player prefers support and crowd control");
                    break;

                default: // Balanced
                    sb.AppendLine("- STYLE HINT: Player prefers balanced, adaptive tactics");
                    break;
            }

            sb.AppendLine();
            sb.AppendLine("DECISION FACTORS (decide priorities yourself based on situation):");
            sb.AppendLine("- Which enemies are the biggest threat RIGHT NOW?");
            sb.AppendLine("- Are any allies in critical danger?");
            sb.AppendLine("- What's the most efficient use of AP this turn?");
            sb.AppendLine("- Should I focus fire to eliminate or spread damage?");
            sb.AppendLine("- Is positioning/cover more important than attacking?");

            // Low health warning
            if (state.CurrentUnit?.HealthPercent <= settings.DefensiveHealthThreshold)
            {
                sb.AppendLine();
                sb.AppendLine($"⚠️ CRITICAL: Current unit at {state.CurrentUnit.HealthPercent}% HP!");
            }

            // Heroic acts reminder
            if (settings.UseHeroicActs && state.Momentum >= 100)
            {
                sb.AppendLine();
                sb.AppendLine($"💫 MOMENTUM: {state.Momentum} - Heroic Acts available if needed!");
            }
        }
    }
}
