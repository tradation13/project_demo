using IPTS.Models.Enums;
using IPTS.Models.Entites;
using Microsoft.AspNetCore.Identity;

namespace IPTS.Models.Entites
{
    public class AppUser : IdentityUser
    {
        public int? UserTypeId { get; set; }
        public UserType? UserType { get; set; }
        public EnUserStatus? Status { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Admin? Admin { get; set; }
        public Doctor? Doctor { get; set; }
        public Patient? Patient { get; set; }

        /// <summary>Accepted privacy policy (required at registration). Default true per product decision.</summary>
        public bool AcceptedPrivacyPolicy { get; set; } = true;

        /// <summary>Accepted terms of use (required at registration). Default true per product decision.</summary>
        public bool AcceptedTermsOfUse { get; set; } = true;

        /// <summary>
        /// Whether AI chat history may be persisted for this account.
        /// Authority for authenticated users (guests use session/localStorage).
        /// Default true per product decision; user can turn off in privacy settings.
        /// </summary>
        public bool ChatHistoryEnabled { get; set; } = true;

        /// <summary>
        /// Explicit Art. 9 / health-data processing consent (patient registration).
        /// Must stay false until the user checks the box — never pre-ticked.
        /// </summary>
        public bool AcceptedHealthDataConsent { get; set; } = false;

        /// <summary>UTC time when health-data consent was given (DSGVO evidence).</summary>
        public DateTime? HealthDataConsentAcceptedAt { get; set; }
    }
}
