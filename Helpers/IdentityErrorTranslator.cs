using IPTS.Resources;
using Microsoft.AspNetCore.Identity;

namespace IPTS.Helpers
{
    /// <summary>
    /// Helper class لترجمة رسائل الأخطاء من ASP.NET Identity
    /// </summary>
    public class IdentityErrorTranslator
    {
        private readonly LocService _locService;

        public IdentityErrorTranslator(LocService locService)
        {
            _locService = locService;
        }

        /// <summary>
        /// ترجمة رسائل الأخطاء من Identity إلى اللغة المطلوبة
        /// </summary>
        public string TranslateErrorMessage(string englishErrorMessage)
        {
            if (string.IsNullOrEmpty(englishErrorMessage))
                return englishErrorMessage;

            // ترجمة الأخطاء الشائعة من Identity
            return englishErrorMessage switch
            {
                // Username errors
                var msg when msg.Contains("Username") && msg.Contains("already") 
                    => _locService.GetSystem("Error_UsernameTaken"),
                
                // Password errors
                var msg when msg.Contains("Passwords must have at least one uppercase") 
                    => _locService.GetSystem("Error_PasswordRequiresUppercase"),
                
                var msg when msg.Contains("Passwords must have at least one lowercase") 
                    => _locService.GetSystem("Error_PasswordRequiresLowercase"),
                
                var msg when msg.Contains("Passwords must have at least one digit") 
                    => _locService.GetSystem("Error_PasswordRequiresDigit"),
                
                var msg when msg.Contains("Passwords must have at least one non-alphanumeric") 
                    => _locService.GetSystem("Error_PasswordRequiresSpecialChar"),
                
                var msg when msg.Contains("minimum length of") 
                    => _locService.GetSystem("Error_PasswordTooShort"),
                
                // Email errors
                var msg when msg.Contains("Email") && msg.Contains("invalid") 
                    => _locService.GetSystem("Error_InvalidEmailFormat"),
                
                var msg when msg.Contains("Email") && msg.Contains("already") 
                    => _locService.GetSystem("Error_EmailAlreadyInUse"),

                // Incorrect / invalid password messages
                var msg when msg.Contains("Incorrect password") || msg.Contains("invalid password") || msg.Contains("Invalid login attempt")
                    => _locService.GetSystem("Error_IncorrectPassword"),
                
                // Default fallback - return the original message if no translation found
                _ => englishErrorMessage
            };
        }

        /// <summary>
        /// ترجمة جميع الأخطاء من Identity result
        /// </summary>
        public string TranslateErrors(IEnumerable<IdentityError> errors)
        {
            if (errors == null || !errors.Any())
                return string.Empty;

            var translatedErrors = errors
                .Select(e => TranslateErrorMessage(e.Description))
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();

            return string.Join(", ", translatedErrors);
        }

        /// <summary>
        /// ترجمة كل خطأ على حدة في list جديد
        /// </summary>
        public List<string> TranslateErrorsList(IEnumerable<IdentityError> errors)
        {
            if (errors == null || !errors.Any())
                return new List<string>();

            return errors
                .Select(e => TranslateErrorMessage(e.Description))
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();
        }
    }
}
