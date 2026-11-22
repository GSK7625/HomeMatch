using System.Text.RegularExpressions;
using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Modules.Properties.Domain.Entities;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.AdminCatalogs;

internal static class CatalogValidationHelper
{
    private static readonly Regex CodeRegex = new("^[A-Z0-9_-]{1,50}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TryNormalize(string? rawCode, string? rawName, out string code, out string name)
    {
        code = (rawCode ?? string.Empty).Trim().ToUpperInvariant();
        name = (rawName ?? string.Empty).Trim();
        return CodeRegex.IsMatch(code) && name.Length is >= 1 and <= 100;
    }
}

public sealed class GetAdminAmenitiesHandler(IAdminCatalogRepository repo)
{
    public async Task<Result<IReadOnlyList<AmenityResult>>> HandleAsync(CancellationToken cancellationToken = default) =>
        Result.Success(await repo.GetAllAmenitiesAsync(cancellationToken));
}

public sealed record CreateAmenityCommand(string Code, string Name, bool IsActive = true);

public sealed class CreateAmenityHandler(IAdminCatalogRepository repo)
{
    public async Task<Result<AmenityResult>> HandleAsync(CreateAmenityCommand command, CancellationToken cancellationToken = default)
    {
        if (!CatalogValidationHelper.TryNormalize(command.Code, command.Name, out var code, out var name))
            return Result.Failure<AmenityResult>(PropertiesErrors.InvalidCatalog);

        if (await repo.AmenityCodeExistsAsync(code, null, cancellationToken)
            || await repo.AmenityNameExistsAsync(name, null, cancellationToken))
            return Result.Failure<AmenityResult>(PropertiesErrors.DuplicateCatalogEntry);

        var entity = new Amenity { Code = code, Name = name, IsActive = command.IsActive };
        repo.AddAmenity(entity);
        var saved = await repo.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<AmenityResult>(saved.Error);

        return Result.Success(new AmenityResult(entity.Id, entity.Code, entity.Name, entity.IsActive));
    }
}

public sealed record UpdateAmenityCommand(long Id, string? Code, string? Name, bool? IsActive);

public sealed class UpdateAmenityHandler(IAdminCatalogRepository repo)
{
    public async Task<Result<AmenityResult>> HandleAsync(UpdateAmenityCommand command, CancellationToken cancellationToken = default)
    {
        var entity = await repo.GetAmenityByIdAsync(command.Id, cancellationToken);
        if (entity is null) return Result.Failure<AmenityResult>(PropertiesErrors.CatalogNotFound);

        var nextCode = command.Code ?? entity.Code;
        var nextName = command.Name ?? entity.Name;
        if (!CatalogValidationHelper.TryNormalize(nextCode, nextName, out var code, out var name))
            return Result.Failure<AmenityResult>(PropertiesErrors.InvalidCatalog);

        if (await repo.AmenityCodeExistsAsync(code, entity.Id, cancellationToken)
            || await repo.AmenityNameExistsAsync(name, entity.Id, cancellationToken))
            return Result.Failure<AmenityResult>(PropertiesErrors.DuplicateCatalogEntry);

        entity.Code = code;
        entity.Name = name;
        if (command.IsActive.HasValue) entity.IsActive = command.IsActive.Value;

        var saved = await repo.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<AmenityResult>(saved.Error);

        return Result.Success(new AmenityResult(entity.Id, entity.Code, entity.Name, entity.IsActive));
    }
}

public sealed class GetAdminRoomTypesHandler(IAdminCatalogRepository repo)
{
    public async Task<Result<IReadOnlyList<RoomTypeResult>>> HandleAsync(CancellationToken cancellationToken = default) =>
        Result.Success(await repo.GetAllRoomTypesAsync(cancellationToken));
}

public sealed record CreateRoomTypeCommand(string Code, string Name, bool IsActive = true);

public sealed class CreateRoomTypeHandler(IAdminCatalogRepository repo)
{
    public async Task<Result<RoomTypeResult>> HandleAsync(CreateRoomTypeCommand command, CancellationToken cancellationToken = default)
    {
        if (!CatalogValidationHelper.TryNormalize(command.Code, command.Name, out var code, out var name))
            return Result.Failure<RoomTypeResult>(PropertiesErrors.InvalidCatalog);

        if (await repo.RoomTypeCodeExistsAsync(code, null, cancellationToken)
            || await repo.RoomTypeNameExistsAsync(name, null, cancellationToken))
            return Result.Failure<RoomTypeResult>(PropertiesErrors.DuplicateCatalogEntry);

        var now = DateTimeOffset.UtcNow;
        var entity = new RoomTypeCatalog
        {
            Code = code,
            Name = name,
            IsActive = command.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };
        repo.AddRoomType(entity);
        var saved = await repo.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<RoomTypeResult>(saved.Error);

        return Result.Success(new RoomTypeResult(entity.Id, entity.Code, entity.Name, entity.IsActive));
    }
}

public sealed record UpdateRoomTypeCommand(long Id, string? Code, string? Name, bool? IsActive);

public sealed class UpdateRoomTypeHandler(IAdminCatalogRepository repo)
{
    public async Task<Result<RoomTypeResult>> HandleAsync(UpdateRoomTypeCommand command, CancellationToken cancellationToken = default)
    {
        var entity = await repo.GetRoomTypeByIdAsync(command.Id, cancellationToken);
        if (entity is null) return Result.Failure<RoomTypeResult>(PropertiesErrors.CatalogNotFound);

        var nextCode = command.Code ?? entity.Code;
        var nextName = command.Name ?? entity.Name;
        if (!CatalogValidationHelper.TryNormalize(nextCode, nextName, out var code, out var name))
            return Result.Failure<RoomTypeResult>(PropertiesErrors.InvalidCatalog);

        if (await repo.RoomTypeCodeExistsAsync(code, entity.Id, cancellationToken)
            || await repo.RoomTypeNameExistsAsync(name, entity.Id, cancellationToken))
            return Result.Failure<RoomTypeResult>(PropertiesErrors.DuplicateCatalogEntry);

        entity.Code = code;
        entity.Name = name;
        if (command.IsActive.HasValue) entity.IsActive = command.IsActive.Value;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        var saved = await repo.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return Result.Failure<RoomTypeResult>(saved.Error);

        return Result.Success(new RoomTypeResult(entity.Id, entity.Code, entity.Name, entity.IsActive));
    }
}
