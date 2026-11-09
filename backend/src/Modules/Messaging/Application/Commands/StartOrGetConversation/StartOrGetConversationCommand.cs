namespace HomeMatch.Modules.Messaging.Application.Commands.StartOrGetConversation;

public record StartOrGetConversationCommand(
    long InitiatorUserId,
    long ListingId,
    long? RecipientUserId = null,
    string? InitialMessage = null);
