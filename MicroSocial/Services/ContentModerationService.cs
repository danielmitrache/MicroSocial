using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MicroSocial.Services
{
    public class ModerationResult
    {
        public bool IsSafe { get; set; } = true;
        public string Reason { get; set; } = string.Empty;
        public bool Success { get; set; } = false;
        public string? ErrorMessage { get; set; }
    }

    public interface IContentModerationService
    {
        Task<ModerationResult> CheckContentAsync(string text);
    }

    public class ContentModerationService : IContentModerationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<ContentModerationService> _logger;
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
        private const string ModelName = "gemini-2.5-flash-lite";

        public ContentModerationService(HttpClient httpClient, IConfiguration configuration, ILogger<ContentModerationService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GEMINI_KEY"] ?? throw new ArgumentNullException("GEMINI_KEY not found in configuration.");
            _logger = logger;
        }

        public async Task<ModerationResult> CheckContentAsync(string text)
        {
            try
            {
                var prompt = @"Analyze the following text for inappropriate content, specifically looking for insults, hate speech, discriminatory language, or severe profanity.
Respond ONLY with a JSON object in the following format:
{ ""isSafe"": boolean, ""reason"": ""string"" }
Rules:
- set isSafe to false if the text contains hate speech, insults, or severe profanity.
- set isSafe to true otherwise.
- reason should be a short explanation if unsafe, or empty if safe.
- Do not add markdown formatting or explanation. Just the raw JSON.
Text: " + text;

                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var url = $"{BaseUrl}/{ModelName}:generateContent?key={_apiKey}";
                
                _logger.LogInformation("Sending moderation request to Gemini API...");
                // Console.WriteLine($"DEBUG: API Key used: {_apiKey.Substring(0, 5)}..."); // Debug safety
                var response = await _httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"DEBUG: Gemini Response JSON: {responseString}");
                Console.WriteLine($"DEBUG: Gemini Response: {responseString}"); // Force console output

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Gemini API Error: {response.StatusCode} - {responseString}");
                    return new ModerationResult { Success = false, ErrorMessage = $"API Error: {response.StatusCode}" };
                }

                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentElem) &&
                        contentElem.TryGetProperty("parts", out var parts) && 
                        parts.GetArrayLength() > 0)
                    {
                        var textResponse = parts[0].GetProperty("text").GetString();
                        
                        // Sanitize response
                        textResponse = textResponse?.Replace("```json", "").Replace("```", "").Trim();

                        if (!string.IsNullOrEmpty(textResponse))
                        {
                            try
                            {
                                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                var moderationData = JsonSerializer.Deserialize<ModerationResponse>(textResponse, options);

                                if (moderationData != null)
                                {
                                    return new ModerationResult 
                                    { 
                                        IsSafe = moderationData.IsSafe, 
                                        Reason = moderationData.Reason ?? string.Empty, 
                                        Success = true 
                                    };
                                }
                            }
                            catch (JsonException ex) 
                            { 
                                _logger.LogError(ex, $"Failed to parse JSON from Gemini: {textResponse}");
                                return new ModerationResult { Success = false, ErrorMessage = "Failed to parse AI response" };
                            }
                        }
                    }
                }

                return new ModerationResult { Success = false, ErrorMessage = "Invalid response format from API" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in ContentModerationService");
                return new ModerationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        private class ModerationResponse
        {
            public bool IsSafe { get; set; }
            public string? Reason { get; set; }
        }
    }
}
