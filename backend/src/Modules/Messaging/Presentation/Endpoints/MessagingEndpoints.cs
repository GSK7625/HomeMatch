using System.Security.Claims;
using HomeMatch.Modules.Messaging.Application.Commands.MarkConversationRead;
using HomeMatch.Modules.Messaging.Application.Commands.SaveMessageDraft;
using HomeMatch.Modules.Messaging.Application.Commands.SendMessage;
using HomeMatch.Modules.Messaging.Application.Commands.StartOrGetConversation;
using HomeMatch.Modules.Messaging.Application.Commands.UpdateLeadStatus;
using HomeMatch.Modules.Messaging.Application.DTOs;
using HomeMatch.Modules.Messaging.Application.Queries.GetConversationMessages;
using HomeMatch.Modules.Messaging.Application.Queries.GetConversations;
using HomeMatch.Modules.Messaging.Application.Queries.GetLandlordLeads;
using HomeMatch.Modules.Messaging.Domain.Constants;
using HomeMatch.Modules.Messaging.Presentation.Endpoints.Requests;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Messaging.Presentation.Endpoints;

public static class MessagingEndpoints
{
    public static IEndpointRouteBuilder MapMessagingFeatureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/conversations").RequireAuthorization();

        // 1. Tạo hoặc mở cuộc hội thoại về phòng (POST /api/v1/messaging/conversations)
        group.MapPost("", async (
            [FromBody] StartConversationRequest request,
            [FromServices] StartOrGetConversationHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<ConversationDto>(MessagingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<ConversationDto>(
                    Error.Validation("Messaging.ValidationFailed", "Dữ liệu khởi tạo hội thoại không hợp lệ.", validationErrors)));
            }

            var result = await handler.HandleAsync(
                new StartOrGetConversationCommand(
                    InitiatorUserId: userId,
                    ListingId: request.ListingId,
                    RecipientUserId: request.RecipientUserId,
                    InitialMessage: request.InitialMessage),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapFailureResult<ConversationDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("StartOrGetConversation")
        .WithSummary("Khởi tạo hoặc mở cuộc hội thoại liên hệ chủ nhà về phòng cho thuê")
        .Produces<ApiResponse<ConversationDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ConversationDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ConversationDto>>(StatusCodes.Status404NotFound);

        // 2. Lấy danh sách hội thoại của tôi (GET /api/v1/messaging/conversations)
        group.MapGet("", async (
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromServices] GetConversationsHandler handler = default!,
            HttpContext httpContext = default!,
            CancellationToken cancellationToken = default) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<PagedConversationsDto>(MessagingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(
                new GetConversationsQuery(userId, page, pageSize),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapFailureResult<PagedConversationsDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("GetConversations")
        .WithSummary("Lấy danh sách các cuộc hội thoại của người dùng hiện tại")
        .Produces<ApiResponse<PagedConversationsDto>>(StatusCodes.Status200OK);

        // 3. Xem lịch sử tin nhắn trong cuộc hội thoại (GET /api/v1/messaging/conversations/{id}/messages)
        group.MapGet("/{id:long}/messages", async (
            long id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromServices] GetConversationMessagesHandler handler = default!,
            HttpContext httpContext = default!,
            CancellationToken cancellationToken = default) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<PagedMessagesDto>(MessagingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(
                new GetConversationMessagesQuery(id, userId, page, pageSize),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapFailureResult<PagedMessagesDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("GetConversationMessages")
        .WithSummary("Xem lịch sử tin nhắn trong một cuộc hội thoại (kèm tự động đánh dấu đã đọc)")
        .Produces<ApiResponse<PagedMessagesDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<PagedMessagesDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<PagedMessagesDto>>(StatusCodes.Status404NotFound);

        // 4. Gửi tin nhắn trong cuộc hội thoại (POST /api/v1/messaging/conversations/{id}/messages)
        group.MapPost("/{id:long}/messages", async (
            long id,
            [FromBody] SendMessageRequest request,
            [FromServices] SendMessageHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<MessageDto>(MessagingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<MessageDto>(
                    Error.Validation("Messaging.ValidationFailed", "Dữ liệu gửi tin nhắn không hợp lệ.", validationErrors)));
            }

            var result = await handler.HandleAsync(
                new SendMessageCommand(id, userId, request.Content, request.MessageType),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapFailureResult<MessageDto>(result.Error);
            }

            return Results.Created(
                $"/api/v1/messaging/conversations/{id}/messages/{result.Value.Id}",
                ApiResponse.Success(result.Value));
        })
        .WithName("SendMessage")
        .WithSummary("Gửi tin nhắn mới trong cuộc hội thoại")
        .Produces<ApiResponse<MessageDto>>(StatusCodes.Status201Created)
        .Produces<ApiResponse<MessageDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<MessageDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<MessageDto>>(StatusCodes.Status404NotFound);

        // 5. Đánh dấu đã đọc tin nhắn trong cuộc hội thoại (PATCH /api/v1/messaging/conversations/{id}/read)
        group.MapPatch("/{id:long}/read", async (
            long id,
            [FromServices] MarkConversationReadHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<object>(MessagingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(
                new MarkConversationReadCommand(id, userId),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapFailureResult<object>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(new { conversationId = id, markedReadCount = result.Value }));
        })
        .WithName("MarkConversationRead")
        .WithSummary("Đánh dấu tất cả tin nhắn chưa đọc của đối phương trong hội thoại là đã đọc")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // 6. Lưu bản nháp tin nhắn (PUT /api/v1/messaging/conversations/{id}/draft)
        group.MapPut("/{id:long}/draft", async (
            long id,
            [FromBody] SaveMessageDraftRequest request,
            [FromServices] SaveMessageDraftHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<MessageDraftDto>(MessagingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<MessageDraftDto>(
                    Error.Validation("Messaging.ValidationFailed", "Dữ liệu bản nháp không hợp lệ.", validationErrors)));
            }

            var result = await handler.HandleAsync(
                new SaveMessageDraftCommand(id, userId, request.Content, request.Source),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapFailureResult<MessageDraftDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("SaveMessageDraft")
        .WithSummary("Lưu hoặc cập nhật bản nháp tin nhắn trong cuộc hội thoại")
        .Produces<ApiResponse<MessageDraftDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<MessageDraftDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<MessageDraftDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<MessageDraftDto>>(StatusCodes.Status404NotFound);

        // 7. Quản lý danh sách khách quan tâm của Chủ nhà (GET / PATCH /api/v1/messaging/leads)
        var leadsGroup = endpoints.MapGroup("/leads")
            .RequireAuthorization(policy => policy
                .RequireAuthenticatedUser()
                .RequireRole("Landlord", "LANDLORD", "Admin", "ADMIN"));

        leadsGroup.MapGet("", async (
            [FromQuery] long? listingId,
            [FromQuery] long? roomId,
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromServices] GetLandlordLeadsHandler handler = default!,
            HttpContext httpContext = default!,
            CancellationToken cancellationToken = default) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<LandlordLeadsResponseDto>(MessagingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await handler.HandleAsync(
                new GetLandlordLeadsQuery(
                    LandlordId: userId,
                    ListingId: listingId,
                    RoomId: roomId,
                    Status: status,
                    Search: search,
                    Page: page,
                    PageSize: pageSize),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapFailureResult<LandlordLeadsResponseDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("GetLandlordLeads")
        .WithSummary("Lấy danh sách khách quan tâm (Leads) của chủ nhà theo tin đăng/phòng và trạng thái liên hệ")
        .Produces<ApiResponse<LandlordLeadsResponseDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<LandlordLeadsResponseDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<LandlordLeadsResponseDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<LandlordLeadsResponseDto>>(StatusCodes.Status403Forbidden);

        leadsGroup.MapPatch("/{id:long}", async (
            long id,
            [FromBody] UpdateLeadRequest request,
            [FromServices] UpdateLeadStatusHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Json(
                    ApiResponse.Failure<LandlordLeadDto>(MessagingErrors.AuthenticationRequired),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var validationErrors = request.Validate();
            if (validationErrors.Count > 0)
            {
                return Results.BadRequest(ApiResponse.Failure<LandlordLeadDto>(
                    Error.Validation("Messaging.ValidationFailed", "Dữ liệu cập nhật khách quan tâm không hợp lệ.", validationErrors)));
            }

            var result = await handler.HandleAsync(
                new UpdateLeadStatusCommand(
                    LeadId: id,
                    LandlordId: userId,
                    Status: request.Status,
                    Notes: request.Notes,
                    UpdateNotes: request.Notes is not null),
                cancellationToken);

            if (result.IsFailure)
            {
                return MapFailureResult<LandlordLeadDto>(result.Error);
            }

            return Results.Ok(ApiResponse.Success(result.Value));
        })
        .WithName("UpdateLandlordLead")
        .WithSummary("Cập nhật trạng thái liên hệ và ghi chú cho khách quan tâm")
        .Produces<ApiResponse<LandlordLeadDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<LandlordLeadDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<LandlordLeadDto>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiResponse<LandlordLeadDto>>(StatusCodes.Status403Forbidden)
        .Produces<ApiResponse<LandlordLeadDto>>(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static bool TryGetUserId(HttpContext httpContext, out long userId)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.FindFirst("sub")?.Value;
        return long.TryParse(claim, out userId);
    }

    private static IResult MapFailureResult<T>(Error error)
    {
        var statusCode = error.Code switch
        {
            "Messaging.ConversationNotFound" or "Messaging.ListingNotFound" or "Messaging.LeadNotFound" => StatusCodes.Status404NotFound,
            "Messaging.Forbidden" or "Messaging.LeadForbidden" => StatusCodes.Status403Forbidden,
            "Messaging.AuthenticationRequired" => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Json(ApiResponse.Failure<T>(error), statusCode: statusCode);
    }
}
