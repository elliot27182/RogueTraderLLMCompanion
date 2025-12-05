using System;
using System.Collections.Generic;

namespace RogueTraderLLMCompanion.Models
{
    /// <summary>
    /// Represents information about a unit (character or enemy) in combat.
    /// </summary>
    public class UnitInfo
    {
        /// <summary>
        /// Unique identifier for the unit
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name of the unit
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Unit type (e.g., "Warrior", "Psyker", "Chaos Marine")
        /// </summary>
        public string UnitType { get; set; }

        /// <summary>
        /// Archetype/class of the unit
        /// </summary>
        public string Archetype { get; set; }

        #region Health and Resources

        /// <summary>
        /// Current health points
        /// </summary>
        public int CurrentHP { get; set; }

        /// <summary>
        /// Maximum health points
        /// </summary>
        public int MaxHP { get; set; }

        /// <summary>
        /// Health percentage (0-100)
        /// </summary>
        public int HealthPercent => MaxHP > 0 ? (CurrentHP * 100 / MaxHP) : 0;

        /// <summary>
        /// Current temporary hit points
        /// </summary>
        public int TempHP { get; set; }

        /// <summary>
        /// Current wounds suffered
        /// </summary>
        public int Wounds { get; set; }

        /// <summary>
        /// Current action points remaining
        /// </summary>
        public int ActionPoints { get; set; }

        /// <summary>
        /// Maximum action points per turn
        /// </summary>
        public int MaxActionPoints { get; set; }

        /// <summary>
        /// Current movement points remaining (in game units/feet)
        /// </summary>
        public float MovementPoints { get; set; }

        /// <summary>
        /// Maximum movement points per turn
        /// </summary>
        public float MaxMovementPoints { get; set; }

        #endregion

        #region Position and Status

        /// <summary>
        /// Current position on the map
        /// </summary>
        public Position Position { get; set; }

        /// <summary>
        /// Current cover status
        /// </summary>
        public CoverType Cover { get; set; }

        /// <summary>
        /// Whether the unit is prone
        /// </summary>
        public bool IsProne { get; set; }

        /// <summary>
        /// Whether the unit is hidden/stealthed
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Active status effects on the unit
        /// </summary>
        public List<StatusEffect> StatusEffects { get; set; } = new List<StatusEffect>();

        #endregion

        #region Combat Stats

        /// <summary>
        /// Weapon Skill (melee accuracy)
        /// </summary>
        public int WeaponSkill { get; set; }

        /// <summary>
        /// Ballistic Skill (ranged accuracy)
        /// </summary>
        public int BallisticSkill { get; set; }

        /// <summary>
        /// Strength stat
        /// </summary>
        public int Strength { get; set; }

        /// <summary>
        /// Toughness stat
        /// </summary>
        public int Toughness { get; set; }

        /// <summary>
        /// Agility stat
        /// </summary>
        public int Agility { get; set; }

        /// <summary>
        /// Armor value
        /// </summary>
        public int Armor { get; set; }

        /// <summary>
        /// Deflection value
        /// </summary>
        public int Deflection { get; set; }

        /// <summary>
        /// Dodge chance percentage
        /// </summary>
        public int DodgeChance { get; set; }

        #endregion

        #region Abilities and Equipment

        /// <summary>
        /// Available abilities/skills the unit can use
        /// </summary>
        public List<AbilityInfo> Abilities { get; set; } = new List<AbilityInfo>();

        /// <summary>
        /// Equipped weapon(s)
        /// </summary>
        public List<WeaponInfo> Weapons { get; set; } = new List<WeaponInfo>();

        /// <summary>
        /// Available consumable items
        /// </summary>
        public List<ItemInfo> Consumables { get; set; } = new List<ItemInfo>();

        #endregion

        #region Threat Assessment

        /// <summary>
        /// Calculated threat level (for AI prioritization)
        /// </summary>
        public int ThreatLevel { get; set; }

        /// <summary>
        /// Whether this unit can attack at range
        /// </summary>
        public bool HasRangedAttack { get; set; }

        /// <summary>
        /// Whether this unit is a psyker
        /// </summary>
        public bool IsPsyker { get; set; }

        /// <summary>
        /// Whether this unit belongs to the player's faction
        /// </summary>
        public bool IsPlayerFaction { get; set; }

        #endregion

        /// <summary>
        /// Creates a brief summary string for prompt inclusion
        /// </summary>
        public string ToSummary()
        {
            string status = string.Join(", ", StatusEffects.ConvertAll(s => s.Name));
            string statusStr = string.IsNullOrEmpty(status) ? "" : $" [{status}]";
            return $"{Name} ({UnitType}): HP {CurrentHP}/{MaxHP}, AP {ActionPoints}, MP {MovementPoints:F0}ft, Pos {Position}{statusStr}";
        }
    }

    /// <summary>
    /// Information about an active status effect
    /// </summary>
    public class StatusEffect
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int RemainingRounds { get; set; }
        public bool IsDebuff { get; set; }
    }

    /// <summary>
    /// Information about a weapon
    /// </summary>
    public class WeaponInfo
    {
        public string Name { get; set; }
        public string Type { get; set; } // Melee, Ranged, Psychic
        public string Damage { get; set; } // e.g., "2d10+8"
        public float Range { get; set; }
        public int APCost { get; set; }
        public List<string> Traits { get; set; } = new List<string>();
    }

    /// <summary>
    /// Information about a consumable item
    /// </summary>
    public class ItemInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Effect { get; set; }
        public int Quantity { get; set; }
        public int APCost { get; set; }
    }
}
