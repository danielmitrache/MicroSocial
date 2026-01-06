using MicroSocial.Models;

namespace MicroSocial.Services
{
    /// <summary>
    /// Service contract for content moderation
    /// </summary>
    public interface IContentModerationService
    {
        /// <summary>
        /// Analyzes content for inappropriate language and policy violations
        /// </summary>
        /// <param name="content">The text content to analyze</param>
        /// <returns>Moderation result indicating approval status and details</returns>
        Task<ModerationResult> ModerateContentAsync(string content);
    }
}
