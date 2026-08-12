using IPTS.Areas.Admin.ViewsModels;
using IPTS.Helpers;
using IPTS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog.Events;
using System.Security.Claims;

namespace IPTS.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize(Roles = "admin")]
    public class ChatConversationsController(ChatbotService chatbotService) : Controller
    {
        private readonly ChatbotService _chatbotService = chatbotService;

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";

            try
            {
                var (totalCount, conversations) = await _chatbotService.GetConversationsPagedAsync(page, pageSize);

                var model = new ChatConversationListViewModel
                {
                    Page = page < 1 ? 1 : page,
                    PageSize = pageSize < 1 ? 10 : pageSize,
                    TotalCount = totalCount,
                    Items = conversations.Select(c => new ChatConversationListItemViewModel
                    {
                        Id = c.Id,
                        SessionId = c.SessionId,
                        UserId = c.UserId,
                        UserType = c.UserType,
                        IpAddress = c.IpAddress,
                        ConsentGiven = c.ConsentGiven,
                        CreatedAt = c.CreatedAt,
                        LastMessageAt = c.LastMessageAt
                    }).ToList()
                };

                LogHelper.LogWithContext(
                    $"Opened chatbot conversations list. page={model.Page}, total={totalCount}",
                    userId,
                    "Admin",
                    "ChatConversationsController.Index",
                    LogEventLevel.Information);

                return View(model);
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error loading chatbot conversations: {ex.Message}",
                    userId,
                    "Admin",
                    "ChatConversationsController.Index",
                    LogEventLevel.Error);

                return View(new ChatConversationListViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";

            try
            {
                var conversation = await _chatbotService.GetConversationWithMessagesAsync(id);
                if (conversation == null)
                    return NotFound();

                var model = new ChatConversationDetailsViewModel
                {
                    Id = conversation.Id,
                    SessionId = conversation.SessionId,
                    UserId = conversation.UserId,
                    UserType = conversation.UserType,
                    IpAddress = conversation.IpAddress,
                    ConsentGiven = conversation.ConsentGiven,
                    ConsentDate = conversation.ConsentDate,
                    CreatedAt = conversation.CreatedAt,
                    LastMessageAt = conversation.LastMessageAt,
                    Messages = conversation.Messages
                        .Select(m => new ChatMessageItemViewModel
                        {
                            Id = m.Id,
                            SenderType = m.SenderType,
                            Message = m.Message,
                            CreatedAt = m.CreatedAt
                        })
                        .ToList()
                };

                LogHelper.LogWithContext(
                    $"Opened chatbot conversation details. conversationId={id}, messageCount={model.Messages.Count}",
                    userId,
                    "Admin",
                    "ChatConversationsController.Details",
                    LogEventLevel.Information);

                return View(model);
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error loading chatbot conversation details id={id}: {ex.Message}",
                    userId,
                    "Admin",
                    "ChatConversationsController.Details",
                    LogEventLevel.Error);

                return NotFound();
            }
        }
    }
}
