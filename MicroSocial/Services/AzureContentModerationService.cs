using Azure;
using Azure.AI.ContentSafety;
using MicroSocial.Models;

namespace MicroSocial.Services
{
    /// <summary>
    /// Content moderation service using Azure AI Content Safety.
    /// Analyzes text for inappropriate content in multiple languages.
    /// </summary>
    public class AzureContentModerationService : IContentModerationService
    {
        private readonly ContentSafetyClient? _client;
        private readonly ILogger<AzureContentModerationService> _logger;
        private readonly bool _isEnabled;

        public AzureContentModerationService(
            IConfiguration configuration,
            ILogger<AzureContentModerationService> logger)
        {
            _logger = logger;
            _isEnabled = configuration.GetValue<bool>("ContentModeration:Enabled", true);

            var endpoint = configuration["ContentModeration:Endpoint"];
            var apiKey = configuration["ContentModeration:ApiKey"];

            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
            {
                try
                {
                    _client = new ContentSafetyClient(
                        new Uri(endpoint),
                        new AzureKeyCredential(apiKey));
                    
                    _logger.LogInformation("Azure Content Safety initialized successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Azure Content Safety");
                    _isEnabled = false;
                }
            }
            else
            {
                _logger.LogWarning("Content moderation disabled: Azure credentials not configured");
                _isEnabled = false;
            }
        }

        public async Task<ModerationResult> ModerateContentAsync(string content)
        {
            if (!_isEnabled || _client == null)
            {
                _logger.LogWarning("Content moderation bypassed - service not configured");
                return ModerationResult.Approved();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return ModerationResult.Approved();
            }

            try
            {
                var request = new AnalyzeTextOptions(content);
                Response<AnalyzeTextResult> response = await _client.AnalyzeTextAsync(request);

                return ProcessModerationResult(response.Value);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Content Safety API error: {Message}", ex.Message);
                return ModerationResult.Approved(); // Fail open to avoid blocking legitimate content
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected content moderation error");
                return ModerationResult.Approved();
            }
        }

        /// <summary>
        /// Processes Azure AI moderation result and determines if content should be blocked
        /// </summary>
        private ModerationResult ProcessModerationResult(AnalyzeTextResult result)
        {
            var violations = new List<string>();

            // Check each category - severity 2+ indicates inappropriate content
            // Severity levels: 0=Safe, 2=Low, 4=Medium, 6=High
            foreach (var category in result.CategoriesAnalysis)
            {
                if (category.Severity >= 2)
                {
                    violations.Add(MapCategoryToRomanian(category.Category.ToString()));
                }
            }

            if (violations.Any())
            {
                _logger.LogInformation("Content rejected by Azure AI. Categories: {Categories}", 
                    string.Join(", ", violations));

                return ModerationResult.Rejected(
                    "Conținutul tău conține termeni nepotriviți. Te rugăm să reformulezi.",
                    violations.Distinct().ToList());
            }

            return ModerationResult.Approved();
        }

        /// <summary>
        /// Maps Azure category to Romanian user-friendly message
        /// </summary>
        private static string MapCategoryToRomanian(string category)
        {
            return category switch
            {
                "Hate" => "Limbaj urâtor",
                "Violence" => "Conținut violent",
                "SelfHarm" => "Conținut dăunător",
                "Sexual" => "Conținut inadecvat",
                _ => "Conținut nepotrivit"
            };
        }
    }
}
