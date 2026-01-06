namespace MicroSocial.Models
{
    /// <summary>
    /// Represents the result of content moderation analysis
    /// </summary>
    public class ModerationResult
    {
        /// <summary>
        /// Indicates whether the content is approved for publication
        /// </summary>
        public bool IsApproved { get; set; }

        /// <summary>
        /// Human-readable reason if content is rejected
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Categories of violations detected (e.g., "Hate Speech", "Violence")
        /// </summary>
        public List<string> Categories { get; set; } = new();

        /// <summary>
        /// Creates a successful moderation result
        /// </summary>
        public static ModerationResult Approved()
        {
            return new ModerationResult
            {
                IsApproved = true,
                Reason = string.Empty,
                Categories = new List<string>()
            };
        }

        /// <summary>
        /// Creates a rejected moderation result
        /// </summary>
        public static ModerationResult Rejected(string reason, List<string> categories)
        {
            return new ModerationResult
            {
                IsApproved = false,
                Reason = reason,
                Categories = categories
            };
        }
    }
}
