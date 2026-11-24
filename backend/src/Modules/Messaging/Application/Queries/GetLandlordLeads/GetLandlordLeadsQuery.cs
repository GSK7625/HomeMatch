namespace HomeMatch.Modules.Messaging.Application.Queries.GetLandlordLeads;

public sealed record GetLandlordLeadsQuery(
    long LandlordId,
    long? ListingId = null,
    long? RoomId = null,
    string? Status = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);
