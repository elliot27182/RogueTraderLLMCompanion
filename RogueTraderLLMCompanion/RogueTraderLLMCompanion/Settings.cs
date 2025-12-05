using System;
using System.Collections.Generic;
using UnityModManagerNet;
using Newtonsoft.Json;

namespace RogueTraderLLMCompanion
{
    /// <summary>
    /// Mod settings that persist across game sessions.
    /// </summary>
    public class ModSettings : UnityModManager.ModSettings
    {
        #region LLM Provider Settings

        /// <summary>
        /// Selected LLM provider (OpenAI, Anthropic, Google, Local)
        /// </summary>
        public LLMProvider Provider = LLMProvider.Google;

        /// <summary>
        /// API key for the selected provider
        /// </summary>
        public string ApiKey = "";

        /// <summary>
        /// Custom API endpoint URL (for local LLMs or proxies)
        /// </summary>
        public string CustomEndpoint = "http://localhost:11434/api/generate";

        /// <summary>
        /// Model name to use (e.g., "gpt-4o", "claude-3-sonnet", "gemini-2.0-flash")
        /// </summary>
        public string ModelName = "gemini-2.0-flash";

        /// <summary>
        /// Maximum tokens for LLM response
        /// </summary>
        public int MaxTokens = 500;

        /// <summary>
        /// Temperature for LLM generation (0.0 = deterministic, 1.0 = creative)
        /// </summary>
        public float Temperature = 0.3f;

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int TimeoutSeconds = 30;

        #endregion

        #region Companion Control Settings

        /// <summary>
        /// Whether to control all companions automatically
        /// </summary>
        public bool ControlAllCompanions = false;

        /// <summary>
        /// Whether to include the player character in AI control
        /// </summary>
        public bool ControlPlayerCharacter = false;

        /// <summary>
        /// List of companion names that should be AI controlled
        /// </summary>
        public List<string> ControlledCompanions = new List<string>();

        /// <summary>
        /// Execution mode: Auto = execute immediately, Manual = wait for player approval
        /// </summary>
        public ExecutionMode ExecutionMode = ExecutionMode.Manual;

        #endregion

        #region Combat Behavior Settings

        /// <summary>
        /// Combat style preference for the AI (hint, not a strict rule)
        /// </summary>
        public CombatStyle PreferredCombatStyle = CombatStyle.Balanced;

        /// <summary>
        /// Whether to use heroic acts when available
        /// </summary>
        public bool UseHeroicActs = true;

        /// <summary>
        /// Whether to use consumable items
        /// </summary>
        public bool UseConsumables = false;

        /// <summary>
        /// Health percentage threshold to prioritize healing/defense
        /// </summary>
        public int DefensiveHealthThreshold = 30;

        #endregion

        #region Debug Settings

        /// <summary>
        /// Enable verbose debug logging
        /// </summary>
        public bool EnableDebugLogging = false;

        /// <summary>
        /// Log LLM prompts to file
        /// </summary>
        public bool LogPrompts = false;

        /// <summary>
        /// Log LLM responses to file
        /// </summary>
        public bool LogResponses = false;

        #endregion

        /// <summary>
        /// Save settings to disk
        /// </summary>
        public void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        /// <summary>
        /// Load settings from disk
        /// </summary>
        public static ModSettings Load(UnityModManager.ModEntry modEntry)
        {
            return Load<ModSettings>(modEntry);
        }

        public override string GetPath(UnityModManager.ModEntry modEntry)
        {
            return System.IO.Path.Combine(modEntry.Path, "Settings.json");
        }
    }

    /// <summary>
    /// Supported LLM providers
    /// </summary>
    public enum LLMProvider
    {
        OpenAI,
        Anthropic,
        Google,
        Local
    }

    /// <summary>
    /// Combat style preferences for AI behavior
    /// </summary>
    public enum CombatStyle
    {
        Aggressive,  // Focus on dealing damage
        Defensive,   // Focus on survival and support
        Balanced,    // Mix of offense and defense
        Support      // Focus on buffing allies and debuffing enemies
    }


    /// <summary>
    /// Execution mode for AI actions
    /// </summary>
    public enum ExecutionMode
    {
        /// <summary>AI executes actions immediately without player approval</summary>
        Auto,
        /// <summary>AI shows proposed action and waits for player to approve or take control</summary>
        Manual
    }
}
