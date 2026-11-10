namespace HomeMatch.Modules.Leasing.Application.Commands.ConfirmHandover;

public sealed record ConfirmHandoverCommand(
    long ContractId,
    long LandlordId,
    DateOnly? ActualOccupancyEndDate = null);
