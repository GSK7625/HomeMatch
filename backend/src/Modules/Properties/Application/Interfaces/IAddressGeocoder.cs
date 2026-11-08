namespace HomeMatch.Modules.Properties.Application.Interfaces;

public sealed record GeocodedLocation(decimal Latitude, decimal Longitude);

public interface IAddressGeocoder
{
    /// <summary>Returns null when the address cannot be located unambiguously.</summary>
    Task<GeocodedLocation?> FindAsync(string address, CancellationToken cancellationToken);
}
