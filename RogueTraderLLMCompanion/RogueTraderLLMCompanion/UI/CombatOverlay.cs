using System;
using UnityEngine;
using RogueTraderLLMCompanion.Combat;
using RogueTraderLLMCompanion.Core;

namespace RogueTraderLLMCompanion.UI
{
    /// <summary>
    /// In-game combat overlay showing AI status and pending actions.
    /// This is drawn on top of the game screen during combat.
    /// </summary>
    public class CombatOverlay
    {
        private static Texture2D _backgroundTexture;
        private static GUIStyle _boxStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _buttonStyle;
        private static bool _initialized = false;

        private static float _fadeTimer = 0f;
        private static string _lastMessage = "";

        /// <summary>
        /// Draws the combat overlay. Call from OnGUI or similar.
        /// </summary>
        public static void Draw(CombatController controller, ModSettings settings)
        {
            if (controller == null || !controller.IsEnabled || !controller.IsInCombat)
                return;

            InitializeStyles();

            // Calculate overlay position (top-center of screen)
            float overlayWidth = 350f;
            float overlayHeight = 100f;
            float x = (Screen.width - overlayWidth) / 2f;
            float y = 10f;

            // Draw background box
            GUI.Box(new Rect(x, y, overlayWidth, overlayHeight), "", _boxStyle);

            GUILayout.BeginArea(new Rect(x + 10, y + 10, overlayWidth - 20, overlayHeight - 20));
            GUILayout.BeginVertical();

            // Status header
            DrawStatusHeader(controller);

            GUILayout.Space(5);

            // Pending action display
            if (controller.PendingAction != null)
            {
                DrawPendingAction(controller, settings);
            }
            else if (controller.IsProcessing)
            {
                DrawProcessingIndicator();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private static void InitializeStyles()
        {
            if (_initialized) return;

            // Create semi-transparent background texture
            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.15f, 0.85f));
            _backgroundTexture.Apply();

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _backgroundTexture },
                border = new RectOffset(4, 4, 4, 4)
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                richText = true,
                normal = { textColor = Color.white }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12
            };

            _initialized = true;
        }

        private static void DrawStatusHeader(CombatController controller)
        {
            string headerText;
            Color headerColor;

            if (controller.IsProcessing)
            {
                headerText = "🤔 LLM Companion - Analyzing...";
                headerColor = new Color(1f, 0.9f, 0.3f); // Yellow
            }
            else if (controller.IsExecutingAction)
            {
                headerText = "⚔️ LLM Companion - Executing";
                headerColor = new Color(0.3f, 0.9f, 1f); // Cyan
            }
            else if (controller.PendingAction != null)
            {
                headerText = "📋 LLM Companion - Action Ready";
                headerColor = new Color(0.3f, 1f, 0.5f); // Green
            }
            else
            {
                headerText = "👁️ LLM Companion - Active";
                headerColor = Color.white;
            }

            var originalColor = GUI.color;
            GUI.color = headerColor;
            GUILayout.Label(headerText, _labelStyle);
            GUI.color = originalColor;
        }

        private static void DrawPendingAction(CombatController controller, ModSettings settings)
        {
            var action = controller.PendingAction;

            // Action display
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            var actionLabel = new GUIStyle(_labelStyle)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            GUILayout.Label(action.ToDisplayString(), actionLabel);
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Reasoning (if available)
            if (!string.IsNullOrEmpty(action.Reasoning))
            {
                var reasoningStyle = new GUIStyle(_labelStyle)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
                };
                GUILayout.Label($"\"{action.Reasoning}\"", reasoningStyle);
            }

            // Buttons only in Manual mode
            if (settings.ExecutionMode == ExecutionMode.Manual)
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("✓ Approve", _buttonStyle, GUILayout.Width(80)))
                {
                    controller.ConfirmAction();
                }

                GUILayout.Space(10);

                if (GUILayout.Button("🎮 Take Control", _buttonStyle, GUILayout.Width(100)))
                {
                    // Skip AI and let player control this turn
                    controller.SkipTurn();
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawProcessingIndicator()
        {
            // Animated dots
            int dots = (int)(Time.time * 3) % 4;
            string dotsStr = new string('.', dots);

            var thinkingStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 14
            };

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Analyzing battlefield{dotsStr}", thinkingStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Shows a temporary message on the overlay.
        /// </summary>
        public static void ShowMessage(string message, float duration = 2f)
        {
            _lastMessage = message;
            _fadeTimer = duration;
        }

        /// <summary>
        /// Updates the fade timer for temporary messages.
        /// </summary>
        public static void Update(float deltaTime)
        {
            if (_fadeTimer > 0)
            {
                _fadeTimer -= deltaTime;
            }
        }
    }
}
