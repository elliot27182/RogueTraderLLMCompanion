using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RogueTraderLLMCompanion.Models;

namespace RogueTraderLLMCompanion.Core
{
    /// <summary>
    /// Service for communicating with LLM APIs (OpenAI, Anthropic, Google, Local).
    /// </summary>
    public class LLMService
    {
        private readonly ModSettings _settings;
        private readonly HttpClient _httpClient;
        private bool _isProcessing;

        public bool IsProcessing => _isProcessing;
        public string LastError { get; private set; }
        public string LastPrompt { get; private set; }
        public string LastResponse { get; private set; }

        public LLMService(ModSettings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        }

        /// <summary>
        /// Sends a combat state to the LLM and gets an action response.
        /// </summary>
        public async Task<LLMAction> GetActionAsync(CombatState state, CancellationToken cancellationToken = default)
        {
            if (_isProcessing)
            {
                Main.LogWarning("LLM request already in progress");
                return LLMAction.EndTurn("Request already in progress");
            }

            _isProcessing = true;
            LastError = null;

            try
            {
                string prompt = PromptBuilder.BuildCombatPrompt(state, _settings);
                LastPrompt = prompt;

                if (_settings.LogPrompts)
                {
                    Main.LogDebug($"Prompt:\n{prompt}");
                }

                string response = await SendRequestAsync(prompt, cancellationToken);
                LastResponse = response;

                if (_settings.LogResponses)
                {
                    Main.LogDebug($"Response:\n{response}");
                }

                if (string.IsNullOrEmpty(response))
                {
                    return LLMAction.EndTurn("Empty response from LLM");
                }

                return LLMAction.Parse(response);
            }
            catch (TaskCanceledException)
            {
                LastError = "Request timed out";
                Main.LogError(LastError);
                return LLMAction.EndTurn(LastError);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Main.LogError($"LLM request failed: {ex}");
                return LLMAction.EndTurn($"Error: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// Sends a request to the configured LLM provider.
        /// </summary>
        private async Task<string> SendRequestAsync(string prompt, CancellationToken cancellationToken)
        {
            switch (_settings.Provider)
            {
                case LLMProvider.OpenAI:
                    return await SendOpenAIRequestAsync(prompt, cancellationToken);
                case LLMProvider.Anthropic:
                    return await SendAnthropicRequestAsync(prompt, cancellationToken);
                case LLMProvider.Google:
                    return await SendGoogleRequestAsync(prompt, cancellationToken);
                case LLMProvider.Local:
                    return await SendLocalRequestAsync(prompt, cancellationToken);
                default:
                    throw new NotSupportedException($"Provider {_settings.Provider} not supported");
            }
        }

        #region OpenAI

        private async Task<string> SendOpenAIRequestAsync(string prompt, CancellationToken cancellationToken)
        {
            const string endpoint = "https://api.openai.com/v1/chat/completions";

            var request = new
            {
                model = _settings.ModelName,
                messages = new[]
                {
                    new { role = "system", content = GetSystemPrompt() },
                    new { role = "user", content = prompt }
                },
                max_tokens = _settings.MaxTokens,
                temperature = _settings.Temperature
            };

            return await SendChatRequestAsync(endpoint, _settings.ApiKey, request, "Bearer", cancellationToken);
        }

        #endregion

        #region Anthropic

        private async Task<string> SendAnthropicRequestAsync(string prompt, CancellationToken cancellationToken)
        {
            const string endpoint = "https://api.anthropic.com/v1/messages";

            var requestObj = new JObject
            {
                ["model"] = _settings.ModelName,
                ["max_tokens"] = _settings.MaxTokens,
                ["system"] = GetSystemPrompt(),
                ["messages"] = new JArray
                {
                    new JObject
                    {
                        ["role"] = "user",
                        ["content"] = prompt
                    }
                }
            };

            string json = requestObj.ToString();
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                request.Content = content;
                request.Headers.Add("x-api-key", _settings.ApiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Anthropic API error: {response.StatusCode} - {responseBody}");
                }

                var responseObj = JObject.Parse(responseBody);
                return responseObj["content"]?[0]?["text"]?.ToString();
            }
        }

        #endregion

        #region Google

        private async Task<string> SendGoogleRequestAsync(string prompt, CancellationToken cancellationToken)
        {
            string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.ModelName}:generateContent?key={_settings.ApiKey}";

            var requestObj = new JObject
            {
                ["contents"] = new JArray
                {
                    new JObject
                    {
                        ["parts"] = new JArray
                        {
                            new JObject { ["text"] = GetSystemPrompt() + "\n\n" + prompt }
                        }
                    }
                },
                ["generationConfig"] = new JObject
                {
                    ["maxOutputTokens"] = _settings.MaxTokens,
                    ["temperature"] = _settings.Temperature
                }
            };

            string json = requestObj.ToString();
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Google API error: {response.StatusCode} - {responseBody}");
            }

            var responseObj = JObject.Parse(responseBody);
            return responseObj["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
        }

        #endregion

        #region Local (Ollama/LM Studio compatible)

        private async Task<string> SendLocalRequestAsync(string prompt, CancellationToken cancellationToken)
        {
            string endpoint = _settings.CustomEndpoint;

            // Try to detect if it's an OpenAI-compatible endpoint or Ollama
            if (endpoint.Contains("/v1/") || endpoint.Contains("chat/completions"))
            {
                // OpenAI-compatible format (LM Studio, etc.)
                var request = new
                {
                    model = _settings.ModelName,
                    messages = new[]
                    {
                        new { role = "system", content = GetSystemPrompt() },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = _settings.MaxTokens,
                    temperature = _settings.Temperature
                };

                return await SendChatRequestAsync(endpoint, null, request, null, cancellationToken);
            }
            else
            {
                // Ollama format
                var requestObj = new JObject
                {
                    ["model"] = _settings.ModelName,
                    ["prompt"] = GetSystemPrompt() + "\n\n" + prompt,
                    ["stream"] = false,
                    ["options"] = new JObject
                    {
                        ["temperature"] = _settings.Temperature,
                        ["num_predict"] = _settings.MaxTokens
                    }
                };

                string json = requestObj.ToString();
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Local LLM error: {response.StatusCode} - {responseBody}");
                }

                var responseObj = JObject.Parse(responseBody);
                return responseObj["response"]?.ToString();
            }
        }

        #endregion

        #region Helpers

        private async Task<string> SendChatRequestAsync(string endpoint, string apiKey, object request, string authScheme, CancellationToken cancellationToken)
        {
            string json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                httpRequest.Content = content;

                if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(authScheme))
                {
                    httpRequest.Headers.Add("Authorization", $"{authScheme} {apiKey}");
                }

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"API error: {response.StatusCode} - {responseBody}");
                }

                var responseObj = JObject.Parse(responseBody);
                return responseObj["choices"]?[0]?["message"]?["content"]?.ToString();
            }
        }

        private string GetSystemPrompt()
        {
            return @"You are an expert AI combat assistant for Warhammer 40,000: Rogue Trader, a turn-based tactical RPG.

Your role is to analyze the combat situation and choose the optimal action for the current unit.

RESPONSE FORMAT:
You must respond with a JSON object containing your chosen action. Examples:

For attacks/abilities:
{""action"": ""ability"", ""ability_name"": ""Power Sword Strike"", ""target_id"": ""enemy_1"", ""reasoning"": ""Highest damage to wounded target""}

For movement:
{""action"": ""move"", ""target_position"": {""x"": 15.0, ""y"": 10.0}, ""reasoning"": ""Moving to cover""}

For using items:
{""action"": ""use_item"", ""item_name"": ""Medicae Kit"", ""target_id"": ""ally_2"", ""reasoning"": ""Healing wounded ally""}

To end turn:
{""action"": ""end_turn"", ""reasoning"": ""No AP remaining""}

TACTICAL PRINCIPLES:
1. Prioritize eliminating high-threat targets (psykers, heavy weapons)
2. Use cover whenever possible
3. Coordinate attacks to focus fire on wounded enemies
4. Protect wounded allies
5. Use crowd control abilities against grouped enemies
6. Save some AP for opportunity attacks when tactical
7. Consider action economy - some abilities have better AP efficiency

IMPORTANT:
- Only use abilities that are marked as available
- Target IDs are provided in the enemy/ally lists
- Check AP costs before choosing abilities
- Respond ONLY with the JSON object, no additional text";
        }

        #endregion

        /// <summary>
        /// Tests the connection to the configured LLM provider.
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var testState = new CombatState
                {
                    CurrentUnit = new UnitInfo
                    {
                        Name = "Test",
                        ActionPoints = 3
                    }
                };

                var result = await GetActionAsync(testState, CancellationToken.None);
                return result != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
