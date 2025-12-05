using System;
using System.Collections.Generic;
using UnityEngine;
using RogueTraderLLMCompanion.Core;
using RogueTraderLLMCompanion.Combat;

namespace RogueTraderLLMCompanion.UI
{
    /// <summary>
    /// IMGUI-based settings interface for the mod.
    /// Displayed in the Unity Mod Manager window (Ctrl+F10).
    /// </summary>
    public static class ModSettingsUI
    {
        // UI State
        private static bool _showApiKey = false;
        private static bool _showAdvanced = false;
        private static string _testResult = "";
        private static bool _isTesting = false;
        
        // Tab selection
        private static int _selectedTab = 0;
        private static readonly string[] _tabNames = { "LLM Settings", "Companion Control", "Combat Behavior", "Debug" };

        // Scroll positions
        private static Vector2 _companionScrollPos;

        /// <summary>
        /// Main draw method called by UMM.
        /// </summary>
        public static void Draw(ModSettings settings, LLMService llmService, CombatController combatController)
        {
            if (settings == null) return;

            // Status bar
            DrawStatusBar(llmService, combatController);
            
            GUILayout.Space(10);

            // Tab selection
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            
            GUILayout.Space(10);

            // Tab content
            switch (_selectedTab)
            {
                case 0:
                    DrawLLMSettings(settings, llmService);
                    break;
                case 1:
                    DrawCompanionControl(settings, combatController);
                    break;
                case 2:
                    DrawCombatBehavior(settings);
                    break;
                case 3:
                    DrawDebugSettings(settings, llmService);
                    break;
            }
        }

        #region Status Bar

        private static void DrawStatusBar(LLMService llmService, CombatController combatController)
        {
            GUILayout.BeginHorizontal("box");
            
            // Connection status
            string statusText;
            Color statusColor;
            
            if (combatController?.IsInCombat == true)
            {
                if (combatController.IsProcessing)
                {
                    statusText = "🤔 AI Thinking...";
                    statusColor = Color.yellow;
                }
                else if (combatController.IsExecutingAction)
                {
                    statusText = "⚔️ Executing Action";
                    statusColor = Color.cyan;
                }
                else if (combatController.PendingAction != null)
                {
                    statusText = $"📋 Pending: {combatController.PendingAction.ToDisplayString()}";
                    statusColor = Color.green;
                }
                else
                {
                    statusText = "⚔️ In Combat";
                    statusColor = Color.green;
                }
            }
            else
            {
                statusText = "😴 Not in Combat";
                statusColor = Color.white;
            }

            var originalColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label(statusText, GUILayout.ExpandWidth(false));
            GUI.color = originalColor;

            GUILayout.FlexibleSpace();

            // Quick toggles
            if (combatController != null)
            {
                if (GUILayout.Button(combatController.IsEnabled ? "■ Disable AI" : "▶ Enable AI", GUILayout.Width(100)))
                {
                    if (combatController.IsEnabled)
                        combatController.Disable();
                    else
                        combatController.Enable();
                }
            }

            GUILayout.EndHorizontal();
        }

        #endregion

        #region LLM Settings Tab

        private static void DrawLLMSettings(ModSettings settings, LLMService llmService)
        {
            GUILayout.Label("<b>LLM Provider Configuration</b>", new GUIStyle(GUI.skin.label) { richText = true });
            
            GUILayout.BeginVertical("box");

            // Provider selection
            GUILayout.BeginHorizontal();
            GUILayout.Label("Provider:", GUILayout.Width(100));
            int providerIndex = (int)settings.Provider;
            string[] providerNames = { "OpenAI", "Anthropic", "Google", "Local" };
            int newProvider = GUILayout.SelectionGrid(providerIndex, providerNames, 4);
            if (newProvider != providerIndex)
            {
                settings.Provider = (LLMProvider)newProvider;
                UpdateDefaultModel(settings);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // API Key (for cloud providers)
            if (settings.Provider != LLMProvider.Local)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("API Key:", GUILayout.Width(100));
                
                if (_showApiKey)
                {
                    settings.ApiKey = GUILayout.TextField(settings.ApiKey);
                }
                else
                {
                    string masked = string.IsNullOrEmpty(settings.ApiKey) ? "" : new string('*', Math.Min(settings.ApiKey.Length, 20));
                    GUILayout.TextField(masked);
                }
                
                if (GUILayout.Button(_showApiKey ? "Hide" : "Show", GUILayout.Width(50)))
                {
                    _showApiKey = !_showApiKey;
                }
                GUILayout.EndHorizontal();
            }

            // Custom endpoint (for local LLMs)
            if (settings.Provider == LLMProvider.Local)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Endpoint:", GUILayout.Width(100));
                settings.CustomEndpoint = GUILayout.TextField(settings.CustomEndpoint);
                GUILayout.EndHorizontal();
            }

            // Model selection
            GUILayout.BeginHorizontal();
            GUILayout.Label("Model:", GUILayout.Width(100));
            settings.ModelName = GUILayout.TextField(settings.ModelName);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Test connection button
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_isTesting ? "Testing..." : "Test Connection", GUILayout.Width(120)))
            {
                if (!_isTesting)
                {
                    TestConnection(llmService);
                }
            }
            GUILayout.Label(_testResult);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // Advanced settings
            GUILayout.Space(10);
            _showAdvanced = GUILayout.Toggle(_showAdvanced, "Show Advanced Settings");
            
            if (_showAdvanced)
            {
                GUILayout.BeginVertical("box");
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("Max Tokens:", GUILayout.Width(100));
                string tokensStr = GUILayout.TextField(settings.MaxTokens.ToString(), GUILayout.Width(60));
                if (int.TryParse(tokensStr, out int tokens))
                {
                    settings.MaxTokens = Math.Max(100, Math.Min(4000, tokens));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Temperature:", GUILayout.Width(100));
                settings.Temperature = GUILayout.HorizontalSlider(settings.Temperature, 0f, 1f, GUILayout.Width(150));
                GUILayout.Label($"{settings.Temperature:F2}", GUILayout.Width(40));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Timeout (s):", GUILayout.Width(100));
                string timeoutStr = GUILayout.TextField(settings.TimeoutSeconds.ToString(), GUILayout.Width(60));
                if (int.TryParse(timeoutStr, out int timeout))
                {
                    settings.TimeoutSeconds = Math.Max(5, Math.Min(120, timeout));
                }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }
        }

        private static void UpdateDefaultModel(ModSettings settings)
        {
            switch (settings.Provider)
            {
                case LLMProvider.OpenAI:
                    settings.ModelName = "gpt-4o";
                    break;
                case LLMProvider.Anthropic:
                    settings.ModelName = "claude-3-sonnet-20240229";
                    break;
                case LLMProvider.Google:
                    settings.ModelName = "gemini-2.0-flash";
                    break;
                case LLMProvider.Local:
                    settings.ModelName = "llama2";
                    settings.CustomEndpoint = "http://localhost:11434/api/generate";
                    break;
            }
        }

        private static async void TestConnection(LLMService llmService)
        {
            _isTesting = true;
            _testResult = "Testing...";

            try
            {
                bool success = await llmService.TestConnectionAsync();
                _testResult = success ? "✓ Connected!" : "✗ Failed";
            }
            catch (Exception ex)
            {
                _testResult = $"✗ Error: {ex.Message}";
            }
            finally
            {
                _isTesting = false;
            }
        }

        #endregion

        #region Companion Control Tab

        private static void DrawCompanionControl(ModSettings settings, CombatController combatController)
        {
            GUILayout.Label("<b>Companion AI Control</b>", new GUIStyle(GUI.skin.label) { richText = true });
            
            GUILayout.BeginVertical("box");

            // Control all toggle
            bool newControlAll = GUILayout.Toggle(settings.ControlAllCompanions, "Control All Companions");
            if (newControlAll != settings.ControlAllCompanions)
            {
                settings.ControlAllCompanions = newControlAll;
                combatController?.UpdateControlledUnits();
            }

            // Player character toggle
            if (settings.ControlAllCompanions)
            {
                settings.ControlPlayerCharacter = GUILayout.Toggle(settings.ControlPlayerCharacter, 
                    "  └ Include Player Character");
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Individual companion selection
            if (!settings.ControlAllCompanions)
            {
                GUILayout.Label("<b>Select Companions to Control:</b>", new GUIStyle(GUI.skin.label) { richText = true });
                
                GUILayout.BeginVertical("box");
                _companionScrollPos = GUILayout.BeginScrollView(_companionScrollPos, GUILayout.Height(150));

                // Note: In actual implementation, we would get the real party list from the game
                string[] companions = { "Abelard", "Cassia", "Heinrix", "Pasqal", "Idira", "Argenta", "Yrliet", "Marazhai", "Ulfar" };
                
                foreach (var companion in companions)
                {
                    bool isControlled = settings.ControlledCompanions.Contains(companion);
                    bool newIsControlled = GUILayout.Toggle(isControlled, companion);
                    
                    if (newIsControlled != isControlled)
                    {
                        if (newIsControlled)
                        {
                            combatController?.AddControlledCompanion(companion);
                        }
                        else
                        {
                            combatController?.RemoveControlledCompanion(companion);
                        }
                    }
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }

            GUILayout.Space(10);

            // Execution mode
            GUILayout.Label("<b>Execution Mode</b>", new GUIStyle(GUI.skin.label) { richText = true });
            
            GUILayout.BeginVertical("box");
            
            GUILayout.BeginHorizontal();
            
            bool isAuto = settings.ExecutionMode == ExecutionMode.Auto;
            
            var autoStyle = new GUIStyle(GUI.skin.button);
            var manualStyle = new GUIStyle(GUI.skin.button);
            
            if (isAuto)
            {
                autoStyle.fontStyle = FontStyle.Bold;
            }
            else
            {
                manualStyle.fontStyle = FontStyle.Bold;
            }
            
            if (GUILayout.Button("🤖 Auto Mode", autoStyle, GUILayout.Height(35)))
            {
                settings.ExecutionMode = ExecutionMode.Auto;
            }
            
            if (GUILayout.Button("👤 Manual Mode", manualStyle, GUILayout.Height(35)))
            {
                settings.ExecutionMode = ExecutionMode.Manual;
            }
            
            GUILayout.EndHorizontal();
            
            // Mode description
            var descStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = true };
            if (isAuto)
            {
                GUILayout.Label("<color=#88ff88>AI executes actions immediately without waiting for approval.</color>", descStyle);
            }
            else
            {
                GUILayout.Label("<color=#88ccff>AI shows proposed action. You can approve it or take control yourself.</color>", descStyle);
            }
            
            GUILayout.EndVertical();
        }

        #endregion

        #region Combat Behavior Tab

        private static void DrawCombatBehavior(ModSettings settings)
        {
            GUILayout.Label("<b>AI Combat Behavior</b>", new GUIStyle(GUI.skin.label) { richText = true });
            
            GUILayout.BeginVertical("box");

            // Combat style (just a hint)
            GUILayout.Label("Combat Style Hint:");
            var descStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Italic };
            GUILayout.Label("(AI uses this as guidance, not a strict rule)", descStyle);
            
            string[] styles = { "Aggressive", "Defensive", "Balanced", "Support" };
            int styleIndex = (int)settings.PreferredCombatStyle;
            int newStyle = GUILayout.SelectionGrid(styleIndex, styles, 4);
            settings.PreferredCombatStyle = (CombatStyle)newStyle;

            GUILayout.Space(10);

            // Note about autonomous targeting
            var noteStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
            GUILayout.Label("<color=#88ccff>🤖 Target Priority: AI decides autonomously based on battlefield analysis</color>", noteStyle);

            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.Label("<b>Special Actions</b>", new GUIStyle(GUI.skin.label) { richText = true });
            
            GUILayout.BeginVertical("box");
            
            settings.UseHeroicActs = GUILayout.Toggle(settings.UseHeroicActs, "Use Heroic Acts when available");
            settings.UseConsumables = GUILayout.Toggle(settings.UseConsumables, "Use Consumable Items");
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Defensive HP Threshold:", GUILayout.Width(150));
            string thresholdStr = GUILayout.TextField(settings.DefensiveHealthThreshold.ToString(), GUILayout.Width(40));
            GUILayout.Label("%");
            if (int.TryParse(thresholdStr, out int threshold))
            {
                settings.DefensiveHealthThreshold = Math.Max(0, Math.Min(100, threshold));
            }
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
        }

        #endregion

        #region Debug Tab

        private static void DrawDebugSettings(ModSettings settings, LLMService llmService)
        {
            GUILayout.Label("<b>Debug Settings</b>", new GUIStyle(GUI.skin.label) { richText = true });
            
            GUILayout.BeginVertical("box");
            
            settings.EnableDebugLogging = GUILayout.Toggle(settings.EnableDebugLogging, "Enable Debug Logging");
            settings.LogPrompts = GUILayout.Toggle(settings.LogPrompts, "Log LLM Prompts");
            settings.LogResponses = GUILayout.Toggle(settings.LogResponses, "Log LLM Responses");
            
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Last prompt/response display
            GUILayout.Label("<b>Last LLM Interaction:</b>", new GUIStyle(GUI.skin.label) { richText = true });
            
            GUILayout.BeginVertical("box");
            
            if (!string.IsNullOrEmpty(llmService?.LastError))
            {
                GUILayout.Label($"<color=red>Error: {llmService.LastError}</color>", 
                    new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true });
            }
            
            if (!string.IsNullOrEmpty(llmService?.LastResponse))
            {
                GUILayout.Label("Response:");
                GUILayout.TextArea(llmService.LastResponse, GUILayout.Height(100));
            }
            else
            {
                GUILayout.Label("No LLM interactions yet.");
            }
            
            GUILayout.EndVertical();
        }

        #endregion
    }
}
