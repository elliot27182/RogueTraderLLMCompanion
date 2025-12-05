using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueTraderLLMCompanion.Models
{
    /// <summary>
    /// Represents an action parsed from LLM response.
    /// </summary>
    public class LLMAction
    {
        /// <summary>
        /// Type of action to perform
        /// </summary>
        [JsonProperty("action")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ActionType Action { get; set; }

        /// <summary>
        /// Name of the ability to use (for ability/attack actions)
        /// </summary>
        [JsonProperty("ability_name")]
        public string AbilityName { get; set; }

        /// <summary>
        /// ID of the ability blueprint (resolved from name)
        /// </summary>
        [JsonIgnore]
        public string AbilityId { get; set; }

        /// <summary>
        /// Target unit ID for targeted actions
        /// </summary>
        [JsonProperty("target_id")]
        public string TargetId { get; set; }

        /// <summary>
        /// Target position for movement or point-targeted abilities
        /// </summary>
        [JsonProperty("target_position")]
        public PositionData TargetPosition { get; set; }

        /// <summary>
        /// Item to use (for use_item action)
        /// </summary>
        [JsonProperty("item_name")]
        public string ItemName { get; set; }

        /// <summary>
        /// Reasoning for this action (for logging/display)
        /// </summary>
        [JsonProperty("reasoning")]
        public string Reasoning { get; set; }

        /// <summary>
        /// Sequence of actions to perform in order (for complex turns)
        /// </summary>
        [JsonProperty("action_sequence")]
        public List<LLMAction> ActionSequence { get; set; }

        /// <summary>
        /// Priority/confidence score for this action (0-100)
        /// </summary>
        [JsonProperty("confidence")]
        public int Confidence { get; set; } = 100;

        /// <summary>
        /// Whether this action has been validated as legal
        /// </summary>
        [JsonIgnore]
        public bool IsValidated { get; set; }

        /// <summary>
        /// Validation error message if action is invalid
        /// </summary>
        [JsonIgnore]
        public string ValidationError { get; set; }

        /// <summary>
        /// Creates a display string for the action
        /// </summary>
        public string ToDisplayString()
        {
            switch (Action)
            {
                case ActionType.Ability:
                case ActionType.Attack:
                    string target = !string.IsNullOrEmpty(TargetId) ? $" -> {TargetId}" : "";
                    return $"{AbilityName}{target}";
                    
                case ActionType.Move:
                    return TargetPosition != null 
                        ? $"Move to ({TargetPosition.X:F0}, {TargetPosition.Y:F0})" 
                        : "Move";
                    
                case ActionType.UseItem:
                    return $"Use {ItemName}";
                    
                case ActionType.EndTurn:
                    return "End Turn";
                    
                case ActionType.Delay:
                    return "Delay Turn";
                    
                default:
                    return Action.ToString();
            }
        }

        /// <summary>
        /// Creates a default "End Turn" action
        /// </summary>
        public static LLMAction EndTurn(string reason = "No valid actions available")
        {
            return new LLMAction
            {
                Action = ActionType.EndTurn,
                Reasoning = reason,
                IsValidated = true
            };
        }

        /// <summary>
        /// Attempts to parse an LLM response into an action
        /// </summary>
        public static LLMAction Parse(string jsonResponse)
        {
            try
            {
                // Try to extract JSON from the response if it's wrapped in markdown
                string json = ExtractJson(jsonResponse);
                return JsonConvert.DeserializeObject<LLMAction>(json);
            }
            catch (Exception ex)
            {
                Main.LogError($"Failed to parse LLM response: {ex.Message}");
                return EndTurn($"Parse error: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts JSON from a response that might be wrapped in markdown code blocks
        /// </summary>
        private static string ExtractJson(string response)
        {
            if (string.IsNullOrEmpty(response))
                return "{}";

            // Remove markdown code blocks if present
            string cleaned = response.Trim();
            
            if (cleaned.StartsWith("```json"))
                cleaned = cleaned.Substring(7);
            else if (cleaned.StartsWith("```"))
                cleaned = cleaned.Substring(3);
                
            if (cleaned.EndsWith("```"))
                cleaned = cleaned.Substring(0, cleaned.Length - 3);

            cleaned = cleaned.Trim();

            // Find the JSON object
            int start = cleaned.IndexOf('{');
            int end = cleaned.LastIndexOf('}');
            
            if (start >= 0 && end > start)
                return cleaned.Substring(start, end - start + 1);

            return cleaned;
        }
    }

    /// <summary>
    /// Position data for JSON serialization
    /// </summary>
    public class PositionData
    {
        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        public Position ToPosition() => new Position(X, Y);
    }

    /// <summary>
    /// Types of actions the LLM can request
    /// </summary>
    public enum ActionType
    {
        /// <summary>Use an ability (generic)</summary>
        [JsonProperty("ability")]
        Ability,

        /// <summary>Perform an attack</summary>
        [JsonProperty("attack")]
        Attack,

        /// <summary>Move to a position</summary>
        [JsonProperty("move")]
        Move,

        /// <summary>Move then attack/use ability</summary>
        [JsonProperty("move_and_attack")]
        MoveAndAttack,

        /// <summary>Use a consumable item</summary>
        [JsonProperty("use_item")]
        UseItem,

        /// <summary>End the current turn</summary>
        [JsonProperty("end_turn")]
        EndTurn,

        /// <summary>Delay turn to act later in initiative</summary>
        [JsonProperty("delay")]
        Delay,

        /// <summary>Take cover at current position</summary>
        [JsonProperty("take_cover")]
        TakeCover,

        /// <summary>Execute a sequence of actions</summary>
        [JsonProperty("sequence")]
        Sequence
    }
}
