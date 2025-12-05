using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RogueTraderLLMCompanion.Core;
using RogueTraderLLMCompanion.Models;

// Verified game assemblies from ToyBox source:
// https://github.com/xADDBx/ToyBox-RogueTrader/blob/main/ToyBox/Classes/Infrastructure/BaseUnitDataUtils.cs

#if !DEBUG_WITHOUT_GAME
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Mechanics.Entities;
using Kingmaker.UnitLogic.Parts;
#endif

namespace RogueTraderLLMCompanion.Combat
{
    /// <summary>
    /// Main controller for LLM-based combat decision making.
    /// API sources: https://github.com/xADDBx/ToyBox-RogueTrader
    /// </summary>
    public class CombatController
    {
        private readonly LLMService _llmService;
        private readonly ModSettings _settings;
        private readonly CombatStateExtractor _stateExtractor;
        private readonly ActionExecutor _actionExecutor;

        private bool _enabled;
        private bool _inCombat;
        private bool _isProcessing;
        private bool _isExecutingAction;
        private CancellationTokenSource _currentRequestCts;

        // Current action state
        private LLMAction _pendingAction;

#if !DEBUG_WITHOUT_GAME
        private BaseUnitEntity _currentUnit;
#else
        private object _currentUnit;
#endif

        // For tracking controlled units
        private HashSet<string> _controlledUnitIds = new HashSet<string>();

        public bool IsEnabled => _enabled;
        public bool IsInCombat => _inCombat;
        public bool IsProcessing => _isProcessing;
        public bool IsExecutingAction => _isExecutingAction;
        public LLMAction PendingAction => _pendingAction;

        public CombatController(LLMService llmService, ModSettings settings)
        {
            _llmService = llmService;
            _settings = settings;
            _stateExtractor = new CombatStateExtractor();
            _actionExecutor = new ActionExecutor();
        }

        #region Enable/Disable

        public void Enable()
        {
            _enabled = true;
            TurnHooks.Initialize(this);
            Main.Log("Combat Controller enabled");
        }

        public void Disable()
        {
            _enabled = false;
            CancelPendingRequest();
            _pendingAction = null;
            _inCombat = false;
            Main.Log("Combat Controller disabled");
        }

        #endregion

        #region Combat Events

        public void OnCombatStarted()
        {
            if (!_enabled) return;

            _inCombat = true;
            _pendingAction = null;
            UpdateControlledUnits();
            Main.Log("LLM Combat Controller: Combat started");
        }

        public void OnCombatEnded()
        {
            _inCombat = false;
            CancelPendingRequest();
            _pendingAction = null;
            _currentUnit = null;
            
            // Release AI control of all units we were controlling
            AIBrainController.ReleaseAllControl();
            
            Main.Log("LLM Combat Controller: Combat ended");
        }

#if !DEBUG_WITHOUT_GAME
        /// <summary>
        /// Called when a unit's turn starts.
        /// Uses verified API from ToyBox.
        /// Implements "Brain Switch" pattern from G3 analysis - disable default AI first.
        /// </summary>
        public void OnUnitTurnStart(BaseUnitEntity unit)
        {
            if (!_enabled || !_inCombat || unit == null) return;

            try
            {
                _currentUnit = unit;
                
                // BRAIN SWITCH: Disable the unit's default AI so it waits for our commands
                // This is critical - without this, the game's AI will act before we can
                AIBrainController.DisableDefaultAI(unit);
                
                Main.Log($"LLM taking control of: {unit.CharacterName}");
                RequestLLMAction();
            }
            catch (Exception ex)
            {
                Main.LogError($"Error handling turn start: {ex}");
            }
        }

        /// <summary>
        /// Checks if a unit should be controlled by the LLM.
        /// Uses verified API from ToyBox BaseUnitDataUtils.
        /// </summary>
        public bool ShouldControlUnit(BaseUnitEntity unit)
        {
            if (!_enabled || unit == null) return false;

            // Check if unit is in player faction (from BaseUnitDataUtils)
            if (!unit.IsPlayerFaction) return false;

            // Check settings
            if (_settings.ControlAllCompanions)
            {
                // Check if this is the main character
                if (unit.IsMainCharacter && !_settings.ControlPlayerCharacter)
                {
                    return false;
                }
                return true;
            }

            // Check if specifically in the controlled list
            string unitName = unit.CharacterName?.ToLowerInvariant() ?? "";
            string unitId = unit.UniqueId?.ToLowerInvariant() ?? "";

            return _controlledUnitIds.Contains(unitName) ||
                   _controlledUnitIds.Contains(unitId);
        }
#else
        public void OnUnitTurnStart(object unit)
        {
            if (!_enabled || !_inCombat || unit == null) return;
            _currentUnit = unit;
            Main.Log("[DEMO] LLM taking control of unit");
            RequestLLMAction();
        }

        public bool ShouldControlUnit(object unit)
        {
            return _enabled && unit != null;
        }
#endif

        public void OnUnitTurnEnd()
        {
            if (_currentUnit != null)
            {
                _currentUnit = null;
                _pendingAction = null;
                _isExecutingAction = false;
            }
        }

        #endregion

        #region Update Loop

        public void Update(float deltaTime)
        {
            if (!_enabled || !_inCombat) return;

            if (_pendingAction != null && !_isExecutingAction)
            {
                if (_settings.ExecutionMode == ExecutionMode.Manual)
                {
                    // In Manual mode, wait for user confirmation
                    return;
                }

                // In Auto mode, execute immediately
                ExecutePendingAction();
            }
        }

        #endregion

        #region LLM Request

        private async void RequestLLMAction()
        {
            if (_isProcessing)
            {
                Main.LogWarning("Already processing an LLM request");
                return;
            }

            _isProcessing = true;
            _currentRequestCts = new CancellationTokenSource();

            try
            {
                var state = _stateExtractor.ExtractState();
                if (state == null)
                {
                    Main.LogError("Failed to extract combat state");
                    _isProcessing = false;
                    return;
                }

                Main.LogDebug($"Requesting LLM action for: {state.CurrentUnit?.Name ?? "Unknown"}");

                var action = await _llmService.GetActionAsync(state, _currentRequestCts.Token);

                if (_currentRequestCts.IsCancellationRequested)
                {
                    Main.LogDebug("LLM request was cancelled");
                    return;
                }

                action = _actionExecutor.ValidateAction(action, state);
                _pendingAction = action;

                Main.Log($"LLM decided: {action.ToDisplayString()}");
                if (!string.IsNullOrEmpty(action.Reasoning))
                {
                    Main.LogDebug($"Reasoning: {action.Reasoning}");
                }
            }
            catch (Exception ex)
            {
                Main.LogError($"LLM request failed: {ex.Message}");
                _pendingAction = LLMAction.EndTurn("LLM request failed");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void CancelPendingRequest()
        {
            _currentRequestCts?.Cancel();
            _currentRequestCts = null;
            _isProcessing = false;
        }

        #endregion

        #region Action Execution

        private void ExecutePendingAction()
        {
            if (_pendingAction == null || _currentUnit == null) return;

            _isExecutingAction = true;

            try
            {
                bool success = _actionExecutor.ExecuteAction(_pendingAction, _currentUnit);

                if (success)
                {
                    Main.LogDebug($"Action executed successfully: {_pendingAction.ToDisplayString()}");
                }
                else
                {
                    Main.LogWarning($"Action execution failed: {_pendingAction.ToDisplayString()}");
                }

                // Check if there are more actions to take
                if (HasActionsRemaining())
                {
                    _pendingAction = null;
                    _isExecutingAction = false;
                    RequestLLMAction();
                }
                else
                {
                    _actionExecutor.EndTurn(_currentUnit);
                    _pendingAction = null;
                    _isExecutingAction = false;
                }
            }
            catch (Exception ex)
            {
                Main.LogError($"Error executing action: {ex}");
                _pendingAction = null;
                _isExecutingAction = false;
            }
        }

        public void ConfirmAction()
        {
            if (_pendingAction != null)
            {
                ExecutePendingAction();
            }
        }

        public void CancelAction()
        {
            if (_currentUnit != null)
            {
                _actionExecutor.EndTurn(_currentUnit);
            }
            _pendingAction = null;
            _isExecutingAction = false;
        }

        public void SkipTurn()
        {
            CancelPendingRequest();
            _pendingAction = null;
            _currentUnit = null;
        }

        #endregion

        #region Unit Control

        public void UpdateControlledUnits()
        {
            _controlledUnitIds.Clear();

            if (_settings.ControlAllCompanions)
            {
                Main.LogDebug("Controlling all companions");
            }
            else
            {
                foreach (var name in _settings.ControlledCompanions)
                {
                    _controlledUnitIds.Add(name.ToLowerInvariant());
                }
            }
        }

        public void AddControlledCompanion(string name)
        {
            if (!_settings.ControlledCompanions.Contains(name))
            {
                _settings.ControlledCompanions.Add(name);
            }
            _controlledUnitIds.Add(name.ToLowerInvariant());
        }

        public void RemoveControlledCompanion(string name)
        {
            _settings.ControlledCompanions.Remove(name);
            _controlledUnitIds.Remove(name.ToLowerInvariant());
        }

        #endregion

        #region Helper Methods

        private bool HasActionsRemaining()
        {
#if !DEBUG_WITHOUT_GAME
            if (_currentUnit == null) return false;

            // Check action points using PartUnitCombatState
            var combatState = _currentUnit.Parts.Get<PartUnitCombatState>();
            return combatState?.ActionPointsYellow > 0;
#else
            return false;
#endif
        }

        #endregion
    }
}
