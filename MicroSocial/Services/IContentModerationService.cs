using MicroSocial.Models;

namespace MicroSocial.Services
{
    public interface IContentModerationService
    {
        Task<ModerationResult> ModerateContentAsync(string content);
    }
}
