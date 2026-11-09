using System.Security.Claims;
using HomeMatch.Modules.Messaging.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Messaging.Presentation.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IConversationRepository conversationRepository,
        ILogger<ChatHub> logger)
    {
        _conversationRepository = conversationRepository;
        _logger = logger;
    }

    public static string GetUserGroup(long userId) => $"user:{userId}";
    public static string GetConversationGroup(long conversationId) => $"conversation:{conversationId}";

    public override async Task OnConnectedAsync()
    {
        if (TryGetCurrentUserId(out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroup(userId));
            _logger.LogDebug("User {UserId} connected to ChatHub (ConnectionId: {ConnectionId})", userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetCurrentUserId(out var userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetUserGroup(userId));
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(long conversationId)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            throw new HubException("Chưa xác thực danh tính người dùng.");
        }

        var conversation = await _conversationRepository.GetByIdAsync(conversationId, Context.ConnectionAborted);
        if (conversation is null
            || (conversation.InitiatorId != userId && conversation.RecipientId != userId))
        {
            throw new HubException("Bạn không có quyền tham gia kênh hội thoại này.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroup(conversationId));
    }

    public async Task LeaveConversation(long conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetConversationGroup(conversationId));
    }

    public async Task SendTypingIndicator(long conversationId, bool isTyping)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return;
        }

        await Clients.OthersInGroup(GetConversationGroup(conversationId)).SendAsync(
            "UserTyping",
            new
            {
                conversationId,
                userId,
                isTyping,
                timestamp = DateTimeOffset.UtcNow
            },
            Context.ConnectionAborted);
    }

    private bool TryGetCurrentUserId(out long userId)
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? Context.User?.FindFirst("sub")?.Value;
        return long.TryParse(claim, out userId);
    }
}
