using System;
using System.Collections.Generic;
using System.Linq;
using RogueTraderLLMCompanion.Models;

// Verified game assemblies from ToyBox source:
// https://github.com/xADDBx/ToyBox-RogueTrader/blob/main/ToyBox/Classes/Infrastructure/BaseUnitDataUtils.cs
// https://github.com/xADDBx/ToyBox-RogueTrader/blob/main/ToyBox/Classes/MonkeyPatchin/BagOfPatches/Combat/ActionsRT.cs

#if !DEBUG_WITHOUT_GAME
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Mechanics.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Controllers.TurnBased;
using Kingmaker.Enums;
using UnityEngine;
#endif

namespace RogueTraderLLMCompanion.Combat
{
    /// <summary>
    /// Extracts combat state information from the game for LLM processing.
    /// API sources: https://github.com/xADDBx/ToyBox-RogueTrader
    /// </summary>
    public class CombatStateExtractor
    {
        /// <summary>
        /// Extracts the current combat state from the game.
        /// </summary>
        public CombatState ExtractState()
        {
            try
            {
#if DEBUG_WITHOUT_GAME
                return GetDemoState();
#else
                var game = Game.Instance;
                if (game == null) return null;

                var state = new CombatState
                {
                    RoundNumber = GetCurrentRound(),
                    CurrentUnit = ExtractCurrentUnit(),
                    FriendlyUnits = ExtractFriendlyUnits(),
                    EnemyUnits = ExtractEnemyUnits(),
                    TurnOrder = ExtractTurnOrder(),
                    Momentum = GetMomentum(),
                    DesperateMeasure = GetDesperateMeasure(),
                    Environment = ExtractEnvironment(),
                    Difficulty = GetDifficulty()
                };

                return state;
#endif
            }
            catch (Exception ex)
            {
                Main.LogError($"Failed to extract combat state: {ex}");
                return null;
            }
        }

#if !DEBUG_WITHOUT_GAME
        #region Game State Extraction

        private int GetCurrentRound()
        {
            // TurnController access pattern from ToyBox
            var turnController = Game.Instance?.TurnController;
            return turnController?.RoundNumber ?? 1;
        }

        private int GetMomentum()
        {
            // Player.Momentum access
            return Game.Instance?.Player?.Momentum?.Value ?? 0;
        }

        private int GetDesperateMeasure()
        {
            return Game.Instance?.Player?.DesperateMeasure?.Value ?? 0;
        }

        private string GetDifficulty()
        {
            return Game.Instance?.Player?.Difficulty?.ToString() ?? "Normal";
        }

        private UnitInfo ExtractCurrentUnit()
        {
            var turnController = Game.Instance?.TurnController;
            var currentUnit = turnController?.CurrentUnit as BaseUnitEntity;
            if (currentUnit == null) return null;

            return ExtractUnitInfo(currentUnit, fullDetails: true);
        }

        private List<UnitInfo> ExtractFriendlyUnits()
        {
            var units = new List<UnitInfo>();

            // Game.Instance.Player.Party - verified from ToyBox BaseUnitDataUtils
            var party = Game.Instance?.Player?.Party;
            if (party == null) return units;

            foreach (var unit in party)
            {
                if (unit == null || unit.LifeState.IsDead) continue;

                var unitInfo = ExtractUnitInfo(unit, fullDetails: false);
                if (unitInfo != null)
                {
                    units.Add(unitInfo);
                }
            }

            return units;
        }

        private List<UnitInfo> ExtractEnemyUnits()
        {
            var enemies = new List<UnitInfo>();

            // Game.Instance.State.AllUnits - verified from ToyBox ActionsRT
            var allUnits = Game.Instance?.State?.AllUnits;
            if (allUnits == null) return enemies;

            foreach (var unit in allUnits)
            {
                if (unit == null) continue;

                // IsEnemy() extension from BaseUnitDataUtils
                if (unit.IsEnemy() && !unit.LifeState.IsDead)
                {
                    var enemyInfo = ExtractUnitInfo(unit, fullDetails: false);
                    if (enemyInfo != null)
                    {
                        enemyInfo.ThreatLevel = CalculateThreatLevel(enemyInfo);
                        enemies.Add(enemyInfo);
                    }
                }
            }

            // Sort by threat level
            enemies.Sort((a, b) => b.ThreatLevel.CompareTo(a.ThreatLevel));
            return enemies;
        }

        private List<string> ExtractTurnOrder()
        {
            var order = new List<string>();

            var turnController = Game.Instance?.TurnController;
            var turnOrder = turnController?.GetOrderedUnits();
            if (turnOrder == null) return order;

            foreach (var unit in turnOrder)
            {
                if (unit is BaseUnitEntity baseUnit)
                {
                    order.Add(baseUnit.CharacterName ?? "Unknown");
                }
            }

            return order;
        }

        private EnvironmentInfo ExtractEnvironment()
        {
            var env = new EnvironmentInfo();

            var area = Game.Instance?.CurrentlyLoadedArea;
            env.AreaName = area?.Name ?? "Unknown";
            env.IsVoidCombat = area?.IsVoidCombat ?? false;

            // Find nearby cover positions
            env.NearbyCover = FindNearbyCover();

            return env;
        }

        #endregion

        #region Unit Extraction

        private UnitInfo ExtractUnitInfo(BaseUnitEntity unit, bool fullDetails)
        {
            if (unit == null) return null;

            try
            {
                // Health access - verified from BaseUnitDataUtils Kill method
                var health = unit.Health;
                var stats = unit.Stats;

                var unitInfo = new UnitInfo
                {
                    Id = unit.UniqueId ?? "unit_" + unit.GetHashCode(),
                    Name = unit.CharacterName ?? "Unknown",
                    UnitType = unit.IsPlayerFaction ? "Ally" : (unit.IsEnemy() ? "Enemy" : "Neutral"),

                    // Health - using pattern from ToyBox
                    CurrentHP = (int)(health?.MaxHitPoints ?? 0) - (int)(health?.Damage ?? 0),
                    MaxHP = (int)(health?.MaxHitPoints ?? 0),
                    TempHP = 0, // Would need specific stat lookup
                    Wounds = (int)(health?.Wounds ?? 0),

                    // Combat state from PartUnitCombatState
                    ActionPoints = GetActionPoints(unit),
                    MaxActionPoints = GetMaxActionPoints(unit),
                    MovementPoints = GetMovementPoints(unit),
                    MaxMovementPoints = GetMaxMovementPoints(unit),

                    // Position
                    Position = new Position(unit.Position.x, unit.Position.z),

                    // Stats
                    WeaponSkill = GetStatValue(stats, StatType.WarhammerWeaponSkill),
                    BallisticSkill = GetStatValue(stats, StatType.WarhammerBallisticSkill),
                    Strength = GetStatValue(stats, StatType.WarhammerStrength),
                    Toughness = GetStatValue(stats, StatType.WarhammerToughness),
                    Agility = GetStatValue(stats, StatType.WarhammerAgility),

                    // Combat flags
                    IsPlayerFaction = unit.IsPlayerFaction,
                    IsPsyker = IsPsyker(unit),
                    HasRangedAttack = HasRangedWeapon(unit)
                };

                // Status effects from buffs
                unitInfo.StatusEffects = ExtractStatusEffects(unit);

                if (fullDetails)
                {
                    unitInfo.Abilities = ExtractAbilities(unit);
                    unitInfo.Weapons = ExtractWeapons(unit);
                }

                return unitInfo;
            }
            catch (Exception ex)
            {
                Main.LogError($"Failed to extract unit info for {unit?.CharacterName}: {ex.Message}");
                return null;
            }
        }

        // =================================================================
        // VERIFIED APIs from decompiled Code.dll/Kingmaker/Controllers/Combat/PartUnitCombatState.cs
        // Line 123: public int ActionPointsYellow { get; private set; }
        // Line 99:  public float ActionPointsBlue { get; private set; }
        // Line 117: public float ActionPointsBlueMax { get; set; }
        // =================================================================

        private int GetActionPoints(BaseUnitEntity unit)
        {
            // VERIFIED: ActionPointsYellow (line 123 of PartUnitCombatState.cs)
            var combatState = unit.Parts.Get<PartUnitCombatState>();
            return combatState?.ActionPointsYellow ?? 0;
        }

        private int GetMaxActionPoints(BaseUnitEntity unit)
        {
            // VERIFIED: WarhammerInitialAPYellow stat (line 389-395 of PartUnitCombatState.cs)
            var combatState = unit.Parts.Get<PartUnitCombatState>();
            return combatState?.WarhammerInitialAPYellow?.BaseValue ?? 3;
        }

        private float GetMovementPoints(BaseUnitEntity unit)
        {
            // VERIFIED: ActionPointsBlue (line 99 of PartUnitCombatState.cs)
            var combatState = unit.Parts.Get<PartUnitCombatState>();
            return combatState?.ActionPointsBlue ?? 0;
        }

        private float GetMaxMovementPoints(BaseUnitEntity unit)
        {
            // VERIFIED: ActionPointsBlueMax (line 117 of PartUnitCombatState.cs)
            var combatState = unit.Parts.Get<PartUnitCombatState>();
            return combatState?.ActionPointsBlueMax ?? 30f;
        }

        private int GetStatValue(UnitStats stats, StatType statType)
        {
            if (stats == null) return 0;
            return stats.GetStat(statType)?.ModifiedValue ?? 0;
        }

        private bool IsPsyker(BaseUnitEntity unit)
        {
            // Check for psyker progression or abilities
            return unit.Progression?.Classes?.Any(c => 
                c.CharacterClass?.Name?.Contains("Psyker") == true) ?? false;
        }

        private bool HasRangedWeapon(BaseUnitEntity unit)
        {
            // Check equipped weapons
            var body = unit.Body;
            if (body?.PrimaryHand?.Weapon != null)
            {
                return body.PrimaryHand.Weapon.Blueprint?.IsRanged ?? false;
            }
            return false;
        }

        private List<StatusEffect> ExtractStatusEffects(BaseUnitEntity unit)
        {
            var effects = new List<StatusEffect>();

            var buffs = unit.Buffs?.RawFacts;
            if (buffs == null) return effects;

            foreach (var buff in buffs)
            {
                effects.Add(new StatusEffect
                {
                    Name = buff.Blueprint?.Name ?? "Unknown",
                    RemainingRounds = buff.Context?.Duration ?? 0,
                    IsDebuff = buff.Blueprint?.Harmful ?? false
                });
            }

            return effects;
        }

        private List<AbilityInfo> ExtractAbilities(BaseUnitEntity unit)
        {
            var abilities = new List<AbilityInfo>();

            var unitAbilities = unit.Abilities?.RawFacts;
            if (unitAbilities == null) return abilities;

            foreach (var ability in unitAbilities)
            {
                var blueprint = ability.Blueprint as BlueprintAbility;
                if (blueprint == null) continue;

                abilities.Add(new AbilityInfo
                {
                    Id = blueprint.AssetGuid.ToString(),
                    Name = blueprint.Name ?? "Unknown",
                    Description = blueprint.Description ?? "",
                    Type = DetermineAbilityType(blueprint),
                    APCost = GetAbilityAPCost(blueprint),
                    Range = blueprint.Range?.Value ?? 0,
                    IsAvailable = CanUseAbility(unit, ability),
                    CanTargetEnemies = blueprint.CanTargetEnemies,
                    CanTargetAllies = blueprint.CanTargetFriends,
                    CanTargetSelf = blueprint.CanTargetSelf
                });
            }

            return abilities;
        }

        private AbilityType DetermineAbilityType(BlueprintAbility blueprint)
        {
            if (blueprint == null) return AbilityType.Attack;

            // Verified API: Use boolean flags instead of Type enum
            if (blueprint.IsSpell || blueprint.IsPsykerAbility)
                return AbilityType.Spell;
            
            if (blueprint.IsWeaponAbility)
                return AbilityType.Attack;

            if (blueprint.EffectOnAlly == AbilityEffectOnUnit.Helpful)
                return AbilityType.Support;

            if (blueprint.EffectOnEnemy == AbilityEffectOnUnit.Harmful)
                return AbilityType.Attack;

            return AbilityType.Attack;
        }

        private int GetAbilityAPCost(BlueprintAbility blueprint)
        {
            // This would need to check the ability's action type
            return 1; // Default
        }

        private bool CanUseAbility(BaseUnitEntity unit, Ability ability)
        {
            // Check if ability can currently be used
            return ability.IsAvailableForCast ?? false;
        }

        private List<WeaponInfo> ExtractWeapons(BaseUnitEntity unit)
        {
            var weapons = new List<WeaponInfo>();

            var body = unit.Body;
            if (body == null) return weapons;

            if (body.PrimaryHand?.Weapon != null)
            {
                var weapon = body.PrimaryHand.Weapon;
                weapons.Add(new WeaponInfo
                {
                    Name = weapon.Name ?? "Primary Weapon",
                    Type = weapon.Blueprint?.IsRanged == true ? "Ranged" : "Melee",
                    Damage = weapon.Blueprint?.BaseDamage?.ToString() ?? "Unknown"
                });
            }

            return weapons;
        }

        #endregion

        #region Helpers

        private int CalculateThreatLevel(UnitInfo enemy)
        {
            int threat = 50;

            if (enemy.HealthPercent < 25) threat -= 20;
            else if (enemy.HealthPercent > 75) threat += 10;

            if (enemy.IsPsyker) threat += 20;
            if (enemy.HasRangedAttack) threat += 10;

            threat += Math.Min(enemy.WeaponSkill / 5, 10);
            threat += Math.Min(enemy.BallisticSkill / 5, 10);

            return Math.Max(0, Math.Min(100, threat));
        }

        private List<CoverPosition> FindNearbyCover()
        {
            // Would need to query the game's cover system
            return new List<CoverPosition>();
        }

        #endregion

#else
        // Demo state for testing without game
        private CombatState GetDemoState()
        {
            return new CombatState
            {
                RoundNumber = 1,
                CurrentUnit = new UnitInfo
                {
                    Id = "player_1",
                    Name = "Test Character",
                    CurrentHP = 100,
                    MaxHP = 100,
                    ActionPoints = 3,
                    MaxActionPoints = 3
                },
                FriendlyUnits = new List<UnitInfo>(),
                EnemyUnits = new List<UnitInfo>(),
                TurnOrder = new List<string> { "Test Character" },
                Momentum = 0
            };
        }
#endif
    }
}
