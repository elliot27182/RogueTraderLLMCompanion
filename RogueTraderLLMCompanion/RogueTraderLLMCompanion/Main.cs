using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using RogueTraderLLMCompanion.Combat;
using RogueTraderLLMCompanion.Core;
using RogueTraderLLMCompanion.UI;

namespace RogueTraderLLMCompanion
{
    /// <summary>
    /// Main entry point for the LLM Companion mod.
    /// Handles mod initialization, settings, and Harmony patching.
    /// </summary>
    public static class Main
    {
        public static bool Enabled { get; private set; }
        public static ModSettings Settings { get; private set; }
        public static UnityModManager.ModEntry ModEntry { get; private set; }
        
        private static Harmony _harmony;
        private static LLMService _llmService;
        private static CombatController _combatController;

        /// <summary>
        /// Called by Unity Mod Manager when the mod is loaded.
        /// </summary>
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            
            try
            {
                // Load settings
                Settings = ModSettings.Load(modEntry);
                
                // Initialize LLM service
                _llmService = new LLMService(Settings);
                
                // Initialize combat controller
                _combatController = new CombatController(_llmService, Settings);
                
                // Apply Harmony patches
                _harmony = new Harmony(modEntry.Info.Id);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                
                // Register callbacks
                modEntry.OnToggle = OnToggle;
                modEntry.OnGUI = OnGUI;
                modEntry.OnSaveGUI = OnSaveGUI;
                modEntry.OnUpdate = OnUpdate;
                
                Log("LLM Companion Mod loaded successfully!");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to load mod: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Called when the mod is enabled or disabled.
        /// </summary>
        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            
            if (value)
            {
                Log("Mod enabled");
                _combatController?.Enable();
            }
            else
            {
                Log("Mod disabled");
                _combatController?.Disable();
            }
            
            return true;
        }

        /// <summary>
        /// Called every frame to update mod state.
        /// </summary>
        private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            if (!Enabled) return;
            _combatController?.Update(deltaTime);
        }

        /// <summary>
        /// Called to draw the mod's settings GUI.
        /// </summary>
        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            ModSettingsUI.Draw(Settings, _llmService, _combatController);
        }

        /// <summary>
        /// Called when settings need to be saved.
        /// </summary>
        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }

        /// <summary>
        /// Gets the combat controller instance.
        /// </summary>
        public static CombatController GetCombatController() => _combatController;

        /// <summary>
        /// Gets the LLM service instance.
        /// </summary>
        public static LLMService GetLLMService() => _llmService;

        #region Logging

        public static void Log(string message)
        {
            ModEntry?.Logger.Log($"[LLMCompanion] {message}");
        }

        public static void LogWarning(string message)
        {
            ModEntry?.Logger.Warning($"[LLMCompanion] {message}");
        }

        public static void LogError(string message)
        {
            ModEntry?.Logger.Error($"[LLMCompanion] {message}");
        }

        public static void LogDebug(string message)
        {
            if (Settings?.EnableDebugLogging == true)
            {
                ModEntry?.Logger.Log($"[LLMCompanion][DEBUG] {message}");
            }
        }

        #endregion
    }
}
