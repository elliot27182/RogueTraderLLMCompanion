using System;
using System.Collections.Generic;

namespace RogueTraderLLMCompanion.Models
{
    /// <summary>
    /// Represents a complete snapshot of the current combat state for LLM processing.
    /// </summary>
    public class CombatState
    {
        /// <summary>
        /// The current round number
        /// </summary>
        public int RoundNumber { get; set; }

        /// <summary>
        /// The unit whose turn it currently is
        /// </summary>
        public UnitInfo CurrentUnit { get; set; }

        /// <summary>
        /// All friendly units in combat
        /// </summary>
        public List<UnitInfo> FriendlyUnits { get; set; } = new List<UnitInfo>();

        /// <summary>
        /// All enemy units visible in combat
        /// </summary>
        public List<UnitInfo> EnemyUnits { get; set; } = new List<UnitInfo>();

        /// <summary>
        /// Turn order for remaining units this round
        /// </summary>
        public List<string> TurnOrder { get; set; } = new List<string>();

        /// <summary>
        /// Party momentum value (for Heroic Acts)
        /// </summary>
        public int Momentum { get; set; }

        /// <summary>
        /// Desperature Measure points available
        /// </summary>
        public int DesperateMeasure { get; set; }

        /// <summary>
        /// Map/environment information
        /// </summary>
        public EnvironmentInfo Environment { get; set; }

        /// <summary>
        /// Combat difficulty setting
        /// </summary>
        public string Difficulty { get; set; }
    }

    /// <summary>
    /// Information about the combat environment/map
    /// </summary>
    public class EnvironmentInfo
    {
        /// <summary>
        /// Name or description of the area
        /// </summary>
        public string AreaName { get; set; }

        /// <summary>
        /// Available cover positions near the current unit
        /// </summary>
        public List<CoverPosition> NearbyCover { get; set; } = new List<CoverPosition>();

        /// <summary>
        /// Hazards or special terrain features
        /// </summary>
        public List<string> Hazards { get; set; } = new List<string>();

        /// <summary>
        /// Whether this is a void ship combat
        /// </summary>
        public bool IsVoidCombat { get; set; }
    }

    /// <summary>
    /// Represents a cover position on the map
    /// </summary>
    public class CoverPosition
    {
        /// <summary>
        /// Position coordinates
        /// </summary>
        public Position Position { get; set; }

        /// <summary>
        /// Cover type (Full, Half, None)
        /// </summary>
        public CoverType Type { get; set; }

        /// <summary>
        /// Distance from current unit in game units
        /// </summary>
        public float Distance { get; set; }
    }

    /// <summary>
    /// Simple 2D position for map coordinates
    /// </summary>
    public class Position
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Position() { }

        public Position(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float DistanceTo(Position other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public override string ToString() => $"({X:F1}, {Y:F1})";
    }

    /// <summary>
    /// Cover types in the game
    /// </summary>
    public enum CoverType
    {
        None,
        Half,
        Full
    }
}
