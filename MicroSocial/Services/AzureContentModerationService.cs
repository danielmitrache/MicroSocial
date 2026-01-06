using Azure;
using Azure.AI.ContentSafety;
using MicroSocial.Models;

namespace MicroSocial.Services
{
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
                return ModerationResult.Approved();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected content moderation error");
                return ModerationResult.Approved();
            }
        }

        private ModerationResult ProcessModerationResult(AnalyzeTextResult result)
        {
            bool hasViolation = result.CategoriesAnalysis.Any(category => category.Severity >= 2);

            if (hasViolation)
            {
                _logger.LogInformation("Content rejected by Azure AI - inappropriate content detected");

                return ModerationResult.Rejected(
                    "Conținutul tău conține termeni nepotriviți. Te rugăm să reformulezi.",
                    new List<string> { "Conținut nepotrivit" });
            }

            return ModerationResult.Approved();
        }
    }
}
