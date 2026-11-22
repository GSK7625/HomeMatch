using System.ComponentModel.DataAnnotations;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints.Requests;

public sealed record CreateCatalogItemRequest
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    [RegularExpression("^[A-Za-z0-9_-]+$")]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}

public sealed record UpdateCatalogItemRequest
{
    [StringLength(50, MinimumLength = 1)]
    [RegularExpression("^[A-Za-z0-9_-]+$")]
    public string? Code { get; init; }

    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; init; }

    public bool? IsActive { get; init; }
}
