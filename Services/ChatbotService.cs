using IPTS.Data;
using IPTS.Helpers;
using IPTS.Models.Entites;
using IPTS.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog.Events;
using System.Security.Claims;

namespace IPTS.Services
{
    public class ChatbotPersistResult
    {
        public bool Saved { get; init; }
        public bool SkippedDueToConsent { get; init; }
        public bool SkippedDueToInvalidInput { get; init; }
        public bool SkippedDueToIntegrity { get; init; }
        public bool SkippedDueToIdentityMismatch { get; init; }
        public int? ConversationId { get; init; }
        public int? MessageId { get; init; }
    }

    public class ChatbotPreferencesSnapshot
    {
        public bool IsAuthenticated { get; init; }
        public bool AcceptedPrivacyPolicy { get; init; }
        public bool AcceptedTermsOfUse { get; init; }
        public bool ChatHistoryEnabled { get; init; }
    }

    public class ChatbotService
    {
        /// <summary>
        /// AI replies must follow a recent unpaired User message (lightweight integrity guard).
        /// </summary>
        private static readonly TimeSpan AiUserMessageWindow = TimeSpan.FromMinutes(15);

        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChatbotService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Server-side consent authority.
        /// Guests: Session conversation with ConsentGiven.
        /// Authenticated: AppUser.ChatHistoryEnabled AND session ConsentGiven.
        /// </summary>
        public async Task<bool> HasConsentAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            sessionId = sessionId.Trim();

            if (!await IsAccountChatHistoryAllowedAsync())
                return false;

            return await _context.ChatConversations
                .AsNoTracking()
                .AnyAsync(c => c.SessionId == sessionId && c.ConsentGiven);
        }

        /// <summary>
        /// Account-level privacy preferences for the authenticated user.
        /// Guests are not account-backed — returns IsAuthenticated=false.
        /// </summary>
        public async Task<ChatbotPreferencesSnapshot> GetAccountPreferencesAsync()
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return new ChatbotPreferencesSnapshot { IsAuthenticated = false };
            }

            var prefs = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.AcceptedPrivacyPolicy,
                    u.AcceptedTermsOfUse,
                    u.ChatHistoryEnabled
                })
                .FirstOrDefaultAsync();

            if (prefs == null)
            {
                return new ChatbotPreferencesSnapshot { IsAuthenticated = false };
            }

            return new ChatbotPreferencesSnapshot
            {
                IsAuthenticated = true,
                AcceptedPrivacyPolicy = prefs.AcceptedPrivacyPolicy,
                AcceptedTermsOfUse = prefs.AcceptedTermsOfUse,
                ChatHistoryEnabled = prefs.ChatHistoryEnabled
            };
        }

        /// <summary>
        /// Updates ChatHistoryEnabled on the authenticated account only.
        /// Does not create a ChatConversation — that happens lazily on the first saved message.
        /// Privacy/Terms cannot be revoked here.
        /// </summary>
        public async Task<ChatbotPersistResult> SetChatHistoryEnabledAsync(bool enabled, string? sessionId)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return new ChatbotPersistResult { SkippedDueToInvalidInput = true };
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return new ChatbotPersistResult { SkippedDueToInvalidInput = true };
            }

            user.ChatHistoryEnabled = enabled;
            await _context.SaveChangesAsync();

            LogHelper.LogWithContext(
                $"Chat history preference updated. enabled={enabled}",
                userId,
                ResolveUserType().ToString(),
                "ChatbotService.SetChatHistoryEnabledAsync");

            // sessionId intentionally unused: avoid empty conversations on preference toggle.
            return new ChatbotPersistResult { Saved = true };
        }

        public async Task<ChatbotPersistResult> AddUserMessageAsync(string sessionId, string message)
        {
            return await PersistMessageAsync(sessionId, message, ChatbotMessageSender.User);
        }

        public async Task<ChatbotPersistResult> AddAiMessageAsync(string sessionId, string message)
        {
            return await PersistMessageAsync(sessionId, message, ChatbotMessageSender.AI);
        }

        /// <summary>
        /// Marks consent on an existing conversation, or creates an empty consented conversation.
        /// Does not save messages. Does not retroactively persist prior chat content.
        /// Authenticated users must have ChatHistoryEnabled on their account.
        /// </summary>
        public async Task<ChatbotPersistResult> GrantConsentAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return new ChatbotPersistResult { SkippedDueToInvalidInput = true };
            }

            if (!await IsAccountChatHistoryAllowedAsync())
            {
                LogHelper.LogWithContext(
                    $"Chat consent skipped: account chat history disabled. sessionId={sessionId.Trim()}",
                    GetAuthenticatedUserId() ?? string.Empty,
                    ResolveUserType().ToString(),
                    "ChatbotService.GrantConsentAsync",
                    LogEventLevel.Information);

                return new ChatbotPersistResult { SkippedDueToConsent = true };
            }

            sessionId = sessionId.Trim();
            var now = DateTime.UtcNow;
            var conversation = await FindBySessionIdAsync(sessionId);

            if (conversation == null)
            {
                conversation = CreateConversationEntity(sessionId, consentGiven: true, now);
                _context.ChatConversations.Add(conversation);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex) when (IsUniqueSessionViolation(ex))
                {
                    _context.Entry(conversation).State = EntityState.Detached;
                    conversation = await FindBySessionIdAsync(sessionId);
                    if (conversation == null)
                        throw;

                    if (!IsConversationOwnedByCurrentUser(conversation))
                    {
                        LogIdentityMismatch("GrantConsentAsync", conversation.Id, sessionId, conversation.UserId);
                        return new ChatbotPersistResult
                        {
                            SkippedDueToIdentityMismatch = true,
                            ConversationId = conversation.Id
                        };
                    }

                    ApplyConsentIfNeeded(conversation, now);
                    ApplyServerIdentity(conversation);
                    conversation.LastMessageAt = now;
                    await _context.SaveChangesAsync();
                }

                LogHelper.LogWithContext(
                    $"Chat conversation created with consent. conversationId={conversation.Id}, sessionId={sessionId}, userType={conversation.UserType}",
                    conversation.UserId ?? string.Empty,
                    conversation.UserType.ToString(),
                    "ChatbotService.GrantConsentAsync");

                return new ChatbotPersistResult
                {
                    Saved = true,
                    ConversationId = conversation.Id
                };
            }

            if (!IsConversationOwnedByCurrentUser(conversation))
            {
                LogIdentityMismatch("GrantConsentAsync", conversation.Id, sessionId, conversation.UserId);
                return new ChatbotPersistResult
                {
                    SkippedDueToIdentityMismatch = true,
                    ConversationId = conversation.Id
                };
            }

            ApplyConsentIfNeeded(conversation, now);
            ApplyServerIdentity(conversation);
            await _context.SaveChangesAsync();

            LogHelper.LogWithContext(
                $"Chat consent granted. conversationId={conversation.Id}, sessionId={sessionId}",
                conversation.UserId ?? string.Empty,
                conversation.UserType.ToString(),
                "ChatbotService.GrantConsentAsync");

            return new ChatbotPersistResult
            {
                Saved = true,
                ConversationId = conversation.Id
            };
        }

        private async Task<ChatbotPersistResult> PersistMessageAsync(
            string sessionId,
            string message,
            ChatbotMessageSender senderType)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(message))
            {
                return new ChatbotPersistResult { SkippedDueToInvalidInput = true };
            }

            sessionId = sessionId.Trim();
            message = message.Trim();

            // Account preference gate (authenticated). Guests always pass this layer.
            if (!await IsAccountChatHistoryAllowedAsync())
            {
                LogHelper.LogWithContext(
                    $"Chat persistence skipped: account chat history disabled. sessionId={sessionId}, sender={senderType}",
                    GetAuthenticatedUserId() ?? string.Empty,
                    ResolveUserType().ToString(),
                    "ChatbotService.PersistMessageAsync",
                    LogEventLevel.Information);

                return new ChatbotPersistResult { SkippedDueToConsent = true };
            }

            var conversation = await FindBySessionIdAsync(sessionId);
            var isAuthenticated = GetAuthenticatedUserId() != null;

            // Authenticated + ChatHistoryEnabled: create consented conversation lazily on first message.
            if (conversation == null && isAuthenticated)
            {
                var grant = await GrantConsentAsync(sessionId);
                if (grant.SkippedDueToIdentityMismatch)
                    return grant;
                if (grant.SkippedDueToConsent || grant.SkippedDueToInvalidInput)
                    return new ChatbotPersistResult { SkippedDueToConsent = true };

                conversation = await FindBySessionIdAsync(sessionId);
            }

            // Guests (and any remaining miss): require an existing consented conversation.
            if (conversation == null || !conversation.ConsentGiven)
            {
                LogHelper.LogWithContext(
                    $"Chat persistence skipped: consent not granted. sessionId={sessionId}, sender={senderType}",
                    GetAuthenticatedUserId() ?? string.Empty,
                    ResolveUserType().ToString(),
                    "ChatbotService.PersistMessageAsync",
                    LogEventLevel.Information);

                return new ChatbotPersistResult { SkippedDueToConsent = true };
            }

            if (!IsConversationOwnedByCurrentUser(conversation))
            {
                LogIdentityMismatch("PersistMessageAsync", conversation.Id, sessionId, conversation.UserId);
                return new ChatbotPersistResult
                {
                    SkippedDueToIdentityMismatch = true,
                    ConversationId = conversation.Id
                };
            }

            if (senderType == ChatbotMessageSender.AI)
            {
                var integrityOk = await HasValidUserAiSequenceAsync(conversation.Id);
                if (!integrityOk)
                {
                    LogHelper.LogWithContext(
                        $"Chat AI persistence rejected: no valid User→AI sequence. conversationId={conversation.Id}, sessionId={sessionId}",
                        GetAuthenticatedUserId() ?? string.Empty,
                        ResolveUserType().ToString(),
                        "ChatbotService.PersistMessageAsync",
                        LogEventLevel.Warning);

                    return new ChatbotPersistResult
                    {
                        SkippedDueToIntegrity = true,
                        ConversationId = conversation.Id
                    };
                }
            }

            var now = DateTime.UtcNow;
            ApplyServerIdentity(conversation);
            conversation.LastMessageAt = now;

            var chatMessage = new ChatMessage
            {
                ChatConversation = conversation,
                SenderType = senderType,
                Message = message,
                CreatedAt = now
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            LogHelper.LogWithContext(
                $"Chat message persisted. conversationId={conversation.Id}, messageId={chatMessage.Id}, sender={senderType}",
                conversation.UserId ?? string.Empty,
                conversation.UserType.ToString(),
                "ChatbotService.PersistMessageAsync");

            return new ChatbotPersistResult
            {
                Saved = true,
                ConversationId = conversation.Id,
                MessageId = chatMessage.Id
            };
        }

        /// <summary>
        /// Requires a recent User message with no AI message after it for this conversation.
        /// </summary>
        private async Task<bool> HasValidUserAiSequenceAsync(int conversationId)
        {
            var cutoff = DateTime.UtcNow.Subtract(AiUserMessageWindow);

            var lastUser = await _context.ChatMessages
                .AsNoTracking()
                .Where(m =>
                    m.ChatConversationId == conversationId &&
                    m.SenderType == ChatbotMessageSender.User &&
                    m.CreatedAt >= cutoff)
                .OrderByDescending(m => m.CreatedAt)
                .ThenByDescending(m => m.Id)
                .Select(m => new { m.Id, m.CreatedAt })
                .FirstOrDefaultAsync();

            if (lastUser == null)
                return false;

            var hasLaterAi = await _context.ChatMessages
                .AsNoTracking()
                .AnyAsync(m =>
                    m.ChatConversationId == conversationId &&
                    m.SenderType == ChatbotMessageSender.AI &&
                    (m.CreatedAt > lastUser.CreatedAt ||
                     (m.CreatedAt == lastUser.CreatedAt && m.Id > lastUser.Id)));

            return !hasLaterAi;
        }

        private async Task<ChatConversation?> FindBySessionIdAsync(string sessionId)
        {
            return await _context.ChatConversations
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);
        }

        private ChatConversation CreateConversationEntity(string sessionId, bool consentGiven, DateTime now)
        {
            var userType = ResolveUserType();
            var userId = GetAuthenticatedUserId();

            return new ChatConversation
            {
                SessionId = sessionId,
                UserId = userId,
                UserType = userType,
                IpAddress = GetClientIpAddress(),
                ConsentGiven = consentGiven,
                ConsentDate = consentGiven ? now : null,
                CreatedAt = now,
                LastMessageAt = now
            };
        }

        private static bool ApplyConsentIfNeeded(ChatConversation conversation, DateTime now)
        {
            if (conversation.ConsentGiven && conversation.ConsentDate.HasValue)
                return false;

            conversation.ConsentGiven = true;
            if (!conversation.ConsentDate.HasValue)
                conversation.ConsentDate = now;

            return true;
        }

        /// <summary>
        /// Guest may only use Guest conversations (UserId null).
        /// Authenticated users may only use conversations owned by the same UserId.
        /// Never reassigns ownership across identities.
        /// </summary>
        private bool IsConversationOwnedByCurrentUser(ChatConversation conversation)
        {
            var currentUserId = GetAuthenticatedUserId();

            if (currentUserId == null)
                return conversation.UserId == null;

            return string.Equals(conversation.UserId, currentUserId, StringComparison.Ordinal);
        }

        private void LogIdentityMismatch(string operation, int conversationId, string sessionId, string? ownerUserId)
        {
            LogHelper.LogWithContext(
                $"Chat {operation} rejected: session/user mismatch. conversationId={conversationId}, sessionId={sessionId}, ownerUserId={(ownerUserId ?? "guest")}, currentUserId={(GetAuthenticatedUserId() ?? "guest")}",
                GetAuthenticatedUserId() ?? string.Empty,
                ResolveUserType().ToString(),
                $"ChatbotService.{operation}",
                LogEventLevel.Warning);
        }

        private void ApplyServerIdentity(ChatConversation conversation)
        {
            // Only called after ownership checks. Refreshes type/IP for the same owner; does not steal conversations.
            conversation.UserType = ResolveUserType();
            conversation.UserId = GetAuthenticatedUserId();
            conversation.IpAddress = GetClientIpAddress();
        }

        private ChatbotUserType ResolveUserType()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return ChatbotUserType.Guest;

            // Roles in this project: admin, doctor, patient (doctor = Therapist in chatbot history).
            if (user.IsInRole("admin"))
                return ChatbotUserType.Admin;

            if (user.IsInRole("doctor"))
                return ChatbotUserType.Therapist;

            if (user.IsInRole("patient"))
                return ChatbotUserType.Patient;

            return ChatbotUserType.Guest;
        }

        private string? GetAuthenticatedUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        /// <summary>
        /// Guests are always allowed at the account layer (session consent decides).
        /// Authenticated users require AppUser.ChatHistoryEnabled.
        /// </summary>
        private async Task<bool> IsAccountChatHistoryAllowedAsync()
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
                return true;

            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.ChatHistoryEnabled)
                .FirstOrDefaultAsync();
        }

        private string? GetClientIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }

        private static bool IsUniqueSessionViolation(DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException pg)
                return pg.SqlState == PostgresErrorCodes.UniqueViolation;

            return ex.InnerException?.Message.Contains("IX_ChatConversations_SessionId", StringComparison.OrdinalIgnoreCase) == true
                || ex.InnerException?.Message.Contains("23505", StringComparison.OrdinalIgnoreCase) == true;
        }

        // ── Admin read-only queries ──────────────────────────────────────────

        /// <summary>
        /// Conversations that have at least one message (excludes empty consent shells).
        /// </summary>
        public Task<int> GetConversationsWithMessagesCountAsync()
        {
            return _context.ChatConversations
                .AsNoTracking()
                .Where(c => c.Messages.Any())
                .CountAsync();
        }

        public async Task<(int TotalCount, List<ChatConversation> Items)> GetConversationsPagedAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            // Hide empty consent shells — only conversations that actually have messages.
            var query = _context.ChatConversations
                .AsNoTracking()
                .Where(c => c.Messages.Any());

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(c => c.LastMessageAt)
                .ThenByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (totalCount, items);
        }

        public async Task<ChatConversation?> GetConversationWithMessagesAsync(int id)
        {
            return await _context.ChatConversations
                .AsNoTracking()
                .Include(c => c.Messages.OrderBy(m => m.CreatedAt).ThenBy(m => m.Id))
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}
