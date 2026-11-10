using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Requests;

public sealed record ConfirmHandoverRequest(
    DateOnly? ActualOccupancyEndDate = null)
{
    public IReadOnlyList<ErrorDetail> Validate()
    {
        return Array.Empty<ErrorDetail>();
    }
}
