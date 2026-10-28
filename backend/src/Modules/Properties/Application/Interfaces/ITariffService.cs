using HomeMatch.Modules.Properties.Application.DTOs;

namespace HomeMatch.Modules.Properties.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ quản lý đơn giá dịch vụ tiện ích (điện, nước, internet...).
/// Tuân thủ SRP — tách biệt trách nhiệm quản lý Đơn giá.
/// Tuân thủ DIP — Controller phụ thuộc vào interface này.
/// </summary>
public interface ITariffService
{
    /// <summary>Tạo mới đơn giá chung cho bất động sản</summary>
    Task<TariffResponse> CreateAsync(long landlordId, long propertyId, CreateTariffRequest request, CancellationToken cancellationToken);

    /// <summary>Cập nhật đơn giá chưa có hiệu lực (PATCH)</summary>
    Task<TariffResponse> UpdateAsync(long landlordId, long propertyId, long tariffId, UpdateTariffRequest request, CancellationToken cancellationToken);

    /// <summary>Lấy lịch sử các đơn giá chung của bất động sản</summary>
    Task<IReadOnlyList<TariffResponse>> GetHistoryAsync(long landlordId, long propertyId, CancellationToken cancellationToken);

    /// <summary>Lấy danh sách đơn giá chung đang hiệu lực tại ngày chỉ định</summary>
    Task<IReadOnlyList<TariffResponse>> GetEffectiveAsync(long landlordId, long propertyId, DateOnly date, CancellationToken cancellationToken);
}
