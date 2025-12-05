using System;
using System.Collections.Generic;

namespace RogueTraderLLMCompanion.Models
{
    /// <summary>
    /// Represents information about an ability or skill a unit can use.
    /// </summary>
    public class AbilityInfo
    {
        /// <summary>
        /// Unique identifier for the ability (blueprint ID)
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name of the ability
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of what the ability does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Type of ability (Attack, Buff, Debuff, Healing, Movement, etc.)
        /// </summary>
        public AbilityType Type { get; set; }

        #region Costs

        /// <summary>
        /// Action point cost to use
        /// </summary>
        public int APCost { get; set; }

        /// <summary>
        /// Movement point cost to use (if any)
        /// </summary>
        public float MPCost { get; set; }

        /// <summary>
        /// Whether this ability requires momentum
        /// </summary>
        public bool RequiresMomentum { get; set; }

        /// <summary>
        /// Momentum cost (for Heroic Acts)
        /// </summary>
        public int MomentumCost { get; set; }

        #endregion

        #region Targeting

        /// <summary>
        /// Maximum range in game units
        /// </summary>
        public float Range { get; set; }

        /// <summary>
        /// Minimum range in game units (for some ranged abilities)
        /// </summary>
        public float MinRange { get; set; }

        /// <summary>
        /// Area of effect radius (0 for single target)
        /// </summary>
        public float AreaOfEffect { get; set; }

        /// <summary>
        /// Type of targeting required
        /// </summary>
        public TargetingType Targeting { get; set; }

        /// <summary>
        /// Whether this can target enemies
        /// </summary>
        public bool CanTargetEnemies { get; set; }

        /// <summary>
        /// Whether this can target allies
        /// </summary>
        public bool CanTargetAllies { get; set; }

        /// <summary>
        /// Whether this can target self
        /// </summary>
        public bool CanTargetSelf { get; set; }

        /// <summary>
        /// Whether this requires line of sight
        /// </summary>
        public bool RequiresLineOfSight { get; set; }

        #endregion

        #region Effects

        /// <summary>
        /// Expected damage string (e.g., "2d10+8 Rending")
        /// </summary>
        public string Damage { get; set; }

        /// <summary>
        /// Damage type (if applicable)
        /// </summary>
        public string DamageType { get; set; }

        /// <summary>
        /// Healing amount (if applicable)
        /// </summary>
        public string Healing { get; set; }

        /// <summary>
        /// Status effects applied by this ability
        /// </summary>
        public List<string> AppliedEffects { get; set; } = new List<string>();

        #endregion

        #region Availability

        /// <summary>
        /// Whether this ability can be used right now
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Reason the ability cannot be used (if not available)
        /// </summary>
        public string UnavailableReason { get; set; }

        /// <summary>
        /// Cooldown remaining (in rounds)
        /// </summary>
        public int CooldownRemaining { get; set; }

        /// <summary>
        /// Uses remaining (for limited-use abilities)
        /// </summary>
        public int UsesRemaining { get; set; }

        #endregion

        /// <summary>
        /// Creates a brief summary for prompt inclusion
        /// </summary>
        public string ToSummary()
        {
            string cost = $"AP:{APCost}";
            if (MPCost > 0) cost += $", MP:{MPCost:F0}";
            if (MomentumCost > 0) cost += $", Momentum:{MomentumCost}";
            
            string range = Range > 0 ? $"Range:{Range:F0}ft" : "Melee";
            string damage = !string.IsNullOrEmpty(Damage) ? $", Dmg:{Damage}" : "";
            string available = IsAvailable ? "" : $" [UNAVAILABLE: {UnavailableReason}]";
            
            return $"- {Name}: [{cost}] {range}{damage}{available}";
        }
    }

    /// <summary>
    /// Types of abilities
    /// </summary>
    public enum AbilityType
    {
        Attack,
        RangedAttack,
        MeleeAttack,
        PsychicPower,
        Buff,
        Debuff,
        Healing,
        Movement,
        Defensive,
        Utility,
        HeroicAct,
        DesperateMeasure
    }

    /// <summary>
    /// Types of targeting for abilities
    /// </summary>
    public enum TargetingType
    {
        Self,
        SingleTarget,
        AreaOfEffect,
        Cone,
        Line,
        Point,
        AllEnemies,
        AllAllies
    }
}
