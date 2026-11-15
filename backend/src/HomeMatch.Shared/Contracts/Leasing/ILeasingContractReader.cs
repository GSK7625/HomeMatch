namespace HomeMatch.Shared.Contracts.Leasing;

public interface ILeasingContractReader
{
    Task<LeasingContractInfo?> GetContractByIdAsync(long contractId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeasingContractInfo>> GetActiveContractsByLandlordIdAsync(long landlordId, CancellationToken cancellationToken = default);
}
