namespace HomeMatch.Modules.Messaging.Application.Commands.UpdateLeadStatus;

public sealed record UpdateLeadStatusCommand(
    long LeadId,
    long LandlordId,
    string? Status,
    string? Notes,
    bool UpdateNotes);
