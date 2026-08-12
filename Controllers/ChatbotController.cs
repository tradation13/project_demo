using IPTS.Helpers;
using IPTS.Services;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Serilog.Events;

namespace IPTS.Controllers
{
    [EnableRateLimiting("ChatbotPolicy")]
    [Route("api/chatbot")]
    public class ChatbotController : Controller
    {
        private readonly ChatbotService _chatbotService;

        public ChatbotController(ChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [AllowAnonymous]
        [HttpPost("consent")]
        [EnableRateLimiting("ChatbotPolicy")]
        public async Task<IActionResult> GrantConsent([FromBody] ChatbotConsentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(InvalidResponse());

            try
            {
                var result = await _chatbotService.GrantConsentAsync(request.SessionId);
                return Json(Map(result));
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Chatbot consent endpoint failed: {ex.Message}",
                    User?.Identity?.Name ?? "Anonymous",
                    "Public",
                    "ChatbotController.GrantConsent",
                    LogEventLevel.Error);

                return StatusCode(StatusCodes.Status500InternalServerError, new ChatbotPersistResponse
                {
                    Success = false,
                    Message = "Unable to record consent."
                });
            }
        }

        /// <summary>
        /// Account privacy prefs for the current user (auth cookie). Guests get IsAuthenticated=false.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("preferences")]
        [EnableRateLimiting("ChatbotPolicy")]
        public async Task<IActionResult> GetPreferences()
        {
            try
            {
                var prefs = await _chatbotService.GetAccountPreferencesAsync();
                return Json(new ChatbotPreferencesResponse
                {
                    Success = true,
                    IsAuthenticated = prefs.IsAuthenticated,
                    AcceptedPrivacyPolicy = prefs.AcceptedPrivacyPolicy,
                    AcceptedTermsOfUse = prefs.AcceptedTermsOfUse,
                    ChatHistoryEnabled = prefs.ChatHistoryEnabled
                });
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Chatbot preferences GET failed: {ex.Message}",
                    User?.Identity?.Name ?? "Anonymous",
                    "Public",
                    "ChatbotController.GetPreferences",
                    LogEventLevel.Error);

                return StatusCode(StatusCodes.Status500InternalServerError, new ChatbotPreferencesResponse
                {
                    Success = false,
                    Message = "Unable to load preferences."
                });
            }
        }

        /// <summary>
        /// Toggle ChatHistoryEnabled on the authenticated account only.
        /// </summary>
        [Authorize]
        [HttpPost("preferences/chat-history")]
        [EnableRateLimiting("ChatbotPolicy")]
        public async Task<IActionResult> SetChatHistoryPreference([FromBody] ChatbotChatHistoryPreferenceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(InvalidResponse());

            try
            {
                var result = await _chatbotService.SetChatHistoryEnabledAsync(request.Enabled, request.SessionId);
                return Json(Map(result));
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Chatbot chat-history preference update failed: {ex.Message}",
                    User?.Identity?.Name ?? "Anonymous",
                    "Public",
                    "ChatbotController.SetChatHistoryPreference",
                    LogEventLevel.Error);

                return StatusCode(StatusCodes.Status500InternalServerError, new ChatbotPersistResponse
                {
                    Success = false,
                    Message = "Unable to update chat history preference."
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("messages/user")]
        [EnableRateLimiting("ChatbotPolicy")]
        public async Task<IActionResult> SaveUserMessage([FromBody] ChatbotMessageRequest request)
        {
            return await SaveMessageAsync(request, isUser: true);
        }

        [AllowAnonymous]
        [HttpPost("messages/ai")]
        [EnableRateLimiting("ChatbotPolicy")]
        public async Task<IActionResult> SaveAiMessage([FromBody] ChatbotMessageRequest request)
        {
            return await SaveMessageAsync(request, isUser: false);
        }

        private async Task<IActionResult> SaveMessageAsync(ChatbotMessageRequest request, bool isUser)
        {
            if (!ModelState.IsValid)
                return BadRequest(InvalidResponse());

            var actionName = isUser ? "SaveUserMessage" : "SaveAiMessage";

            try
            {
                // Consent is resolved inside ChatbotService via HasConsentAsync / DB — never from the client.
                var result = isUser
                    ? await _chatbotService.AddUserMessageAsync(request.SessionId, request.Message)
                    : await _chatbotService.AddAiMessageAsync(request.SessionId, request.Message);

                return Json(Map(result));
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Chatbot message endpoint failed: {ex.Message}",
                    User?.Identity?.Name ?? "Anonymous",
                    "Public",
                    $"ChatbotController.{actionName}",
                    LogEventLevel.Error);

                return StatusCode(StatusCodes.Status500InternalServerError, new ChatbotPersistResponse
                {
                    Success = false,
                    Message = "Unable to persist message."
                });
            }
        }

        private static ChatbotPersistResponse Map(ChatbotPersistResult result)
        {
            return new ChatbotPersistResponse
            {
                Success = !result.SkippedDueToInvalidInput,
                Saved = result.Saved,
                SkippedDueToConsent = result.SkippedDueToConsent,
                SkippedDueToInvalidInput = result.SkippedDueToInvalidInput,
                SkippedDueToIntegrity = result.SkippedDueToIntegrity,
                SkippedDueToIdentityMismatch = result.SkippedDueToIdentityMismatch,
                ConversationId = result.ConversationId,
                MessageId = result.MessageId
            };
        }

        private static ChatbotPersistResponse InvalidResponse()
        {
            return new ChatbotPersistResponse
            {
                Success = false,
                SkippedDueToInvalidInput = true,
                Message = "Invalid request."
            };
        }
    }
}
