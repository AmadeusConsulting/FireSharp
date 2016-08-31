using System.Collections.Generic;
using System.Threading.Tasks;

namespace FireSharp.GeoFire
{
    public interface IGeoHashingService
    {
        Task<IDictionary<string, LocationSearchResult>> FindLocations(GeographyPoint center, double radiusKm);
    }
}