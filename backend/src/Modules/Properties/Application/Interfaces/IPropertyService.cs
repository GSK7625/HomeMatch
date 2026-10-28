using HomeMatch.Modules.Properties.Application.DTOs;

namespace HomeMatch.Modules.Properties.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ quản lý bất động sản (Căn/Tòa nhà).
/// Tuân thủ Dependency Inversion Principle (DIP) — Controller phụ thuộc vào abstraction này,
/// không phụ thuộc vào class cụ thể PropertyService.
/// </summary>
public interface IPropertyService
{
    /// <summary>Tạo mới một bất động sản cho chủ nhà</summary>
    Task<PropertyResponse> CreateAsync(long landlordId, CreatePropertyRequest request, CancellationToken cancellationToken);

    /// <summary>Lấy danh sách tất cả bất động sản của chủ nhà</summary>
    Task<IReadOnlyList<PropertyResponse>> GetMineAsync(long landlordId, CancellationToken cancellationToken);

    /// <summary>Lấy thông tin chi tiết một bất động sản của chủ nhà</summary>
    Task<PropertyResponse> GetByIdAsync(long landlordId, long propertyId, CancellationToken cancellationToken);

    /// <summary>Cập nhật thông tin bất động sản (PATCH — chỉ cập nhật field được truyền vào)</summary>
    Task<PropertyResponse> UpdateAsync(long landlordId, long propertyId, UpdatePropertyRequest request, CancellationToken cancellationToken);
}
