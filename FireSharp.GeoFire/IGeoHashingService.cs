using System.Collections.Generic;
using System.Threading.Tasks;

namespace FireSharp.GeoFire
{
    public interface IGeoHashingService
    {
        Task<IDictionary<string, LocationSearchResult>> FindLocationsAsync(GeographyPoint center, double radiusKm);

        Task SetLocationAsync(string key, double latitude, double longitude, IDictionary<string, string> metadata = null);
    }
}