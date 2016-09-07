//-------------------------------------------------------------------------
//  This Geohashing implementation is a translation from Geofire-JS
// 
// Original work Copyright (c) 2016 Firebase
// Modified work Copyright (c) 2016 Amadeus Consulting

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//-------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FireSharp.Interfaces;
using FireSharp.Logging;
using FireSharp.Response;

namespace FireSharp.GeoFire
{
    public class GeoHashingService : IGeoHashingService
    {
        #region Constants

        private const string Base32 = "0123456789bcdefghjkmnpqrstuvwxyz";

        private const int BitsPerChar = 5;

        private const int DefaultGeoHashPrecision = 12;

        /// <summary>
        ///     The following value assumes a polar radius of 6356752.3;
        ///     The formulate to calculate E2 is
        ///     E2 == (EarthEquitorialRadius^2-EarthPolarRadius^2)/(EarthEquitorialRadius^2)
        ///     The exact value is used here to avoid rounding errors
        /// </summary>
        private const double E2 = 0.00669447819799;

        private const double EarthEquitorialRadius = 6378137.0;

        private const double EarthMeridianCircumferenceMeters = 40007860.0;

        private const double Epsilon = 1e-12;

        private const int MaxBitsPrecision = 22 * BitsPerChar;

        private const double MetersPerDegreeLatitude = 110574.0; // at the equator

        #endregion

        #region Fields

        private readonly string _basePath;

        private readonly IFirebaseConfig _config;

        private readonly IFirebaseClient _firebaseClient;

        private readonly int _hashingPrecision;

        private readonly ILog _log;

        #endregion

        #region Constructors and Destructors

        public GeoHashingService(
            IFirebaseClient firebaseClient, 
            string basePath, 
            IFirebaseConfig config, 
            int hashingPrecision = DefaultGeoHashPrecision)
        {
            if (firebaseClient == null)
            {
                throw new ArgumentNullException(nameof(firebaseClient));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(basePath))
            {
                throw new ArgumentNullException(nameof(basePath));
            }

            if (hashingPrecision < 1 || hashingPrecision > 20)
            {
                throw new ArgumentOutOfRangeException("hashingPrecision", "Precision must be between 1 and 20 characters");
            }

            if (basePath.EndsWith("/"))
            {
                basePath = basePath.Substring(0, basePath.Length - 1);
            }

            _firebaseClient = firebaseClient;
            _basePath = basePath;
            _config = config;
            _hashingPrecision = hashingPrecision;
            _log = _config.LogManager.GetLogger(this);
        }

        #endregion

        #region Public Methods and Operators

        public async Task<IDictionary<string, LocationSearchResult>> FindLocationsAsync(GeographyPoint center, double radiusKm)
        {
            _log.Debug($"#### Finding locations for center point [{center.Latitude}, {center.Longitude}] with radius of {radiusKm} km ####");

            var geoHashesToQuery = CalculateHashStartAndEndPoints(center, radiusKm * 1000).ToList();

            var executedQueries = new List<Task<FirebaseResponse>>();

            foreach (var queryStartEnd in geoHashesToQuery)
            {
                _log.Debug($"Add query start: {queryStartEnd[0]} end: {queryStartEnd[1]}");

                var queryBuilder = QueryBuilder.New().OrderBy(GeoHashLocation.GeoHashPropertyName).StartAt(queryStartEnd[0]).EndAt(queryStartEnd[1]);

                executedQueries.Add(_firebaseClient.GetAsync(_basePath, queryBuilder));
            }

            var responses = await Task.WhenAll(executedQueries);

            var candidateLocations =
                responses.SelectMany(res => res.ResultAs<IDictionary<string, LocationSearchResult>>().ToList())
                    .Distinct(new KeyValueKeyComparer<string, LocationSearchResult>(StringComparer.CurrentCulture))
                    .ToList();

            foreach (var location in candidateLocations)
            {
                UpdateDistanceToCenter(center, location.Value);
                _log.Debug($"==> Found location {location.Key} ({location.Value.Location.Latitude}, {location.Value.Location.Longitude}) at distance of {location.Value.DistanceToCenterKm} km");
            }

            return candidateLocations.Where(l => l.Value.DistanceToCenterKm <= radiusKm).ToDictionary(k => k.Key, v => v.Value);
        }

        public async Task SetLocationAsync(string key, double latitude, double longitude, IDictionary<string, string> metadata = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            string encodedLocation = Encode(latitude, longitude);

            var location = new GeoHashLocation
                               {
                                   GeoHash = encodedLocation, 
                                   Location = new GeographyPoint
                                                  {
                                                      Latitude = latitude, 
                                                      Longitude = longitude
                                                  }
                               };

            if (metadata != null)
            {
                location.Metadata = metadata;
            }

            await _firebaseClient.SetAsync($"{_basePath}/{key}", location);
        }

        #endregion

        #region Methods

        private int CalculateBoundingBoxBits(GeographyPoint point, double sizeMeters)
        {
            var latitudeDeltaDegrees = sizeMeters / MetersPerDegreeLatitude;
            var latitudeNorth = Math.Min(90, point.Latitude + latitudeDeltaDegrees);
            var latitudeSouth = Math.Max(-90, point.Latitude - latitudeDeltaDegrees);
            var bitsLat = Math.Floor(CalculateLatitudeBitsForResolution(sizeMeters)) * 2;
            var bitsLonNorth = Math.Floor(CalculateLongitudeBitsForResolution(sizeMeters, latitudeNorth)) * 2 - 1;
            var bitsLonSouth = Math.Floor(CalculateLongitudeBitsForResolution(sizeMeters, latitudeSouth)) * 2 - 1;
            return (int)new[] { bitsLat, bitsLonNorth, bitsLonSouth, MaxBitsPrecision }.Min();
        }

        private GeographyPoint[] CalculateBoundingBoxCoordinates(GeographyPoint center, double radiusMeters)
        {
            var latDegrees = radiusMeters / MetersPerDegreeLatitude;
            var latitudeNorth = Math.Min(90, center.Latitude + latDegrees);
            var latitudeSouth = Math.Max(-90, center.Latitude - latDegrees);
            var lonDegreesNorth = MetersToLongitudeDegrees(radiusMeters, latitudeNorth);
            var lonDegreesSouth = MetersToLongitudeDegrees(radiusMeters, latitudeSouth);
            var lonDegrees = Math.Max(lonDegreesNorth, lonDegreesSouth);

            return new[]
                       {
                           center, 
                           new GeographyPoint
                               {
                                   Latitude = center.Latitude, 
                                   Longitude = WrapLongitude(center.Longitude - lonDegrees)
                               }, 
                           new GeographyPoint
                               {
                                   Latitude = center.Latitude, 
                                   Longitude = WrapLongitude(center.Longitude + lonDegrees)
                               }, 
                           new GeographyPoint
                               {
                                   Latitude = latitudeNorth, 
                                   Longitude = center.Longitude
                               }, 
                           new GeographyPoint
                               {
                                   Latitude = latitudeNorth, 
                                   Longitude = WrapLongitude(center.Longitude - lonDegrees)
                               }, 
                           new GeographyPoint
                               {
                                   Latitude = latitudeNorth, 
                                   Longitude = WrapLongitude(center.Longitude + lonDegrees)
                               }, 
                           new GeographyPoint
                               {
                                   Latitude = latitudeSouth, 
                                   Longitude = center.Longitude
                               }, 
                           new GeographyPoint
                               {
                                   Latitude = latitudeSouth, 
                                   Longitude = WrapLongitude(center.Longitude - lonDegrees)
                               }, 
                           new GeographyPoint
                               {
                                   Latitude = latitudeSouth, 
                                   Longitude = WrapLongitude(center.Longitude + lonDegrees)
                               }
                       };
        }

        /// <summary>
        /// </summary>
        /// <param name="center">
        /// </param>
        /// <param name="radiusMeters">
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        /// <remarks>
        /// Created using
        ///     https://github.com/firebase/geofire-js/blob/66292a95a5210b46d72fbc5b588f8c35cfcad665/src/geoFireUtils.js#L404 as a
        ///     reference implementation
        /// </remarks>
        private IEnumerable<string[]> CalculateHashStartAndEndPoints(GeographyPoint center, double radiusMeters)
        {
            if (center == null)
            {
                throw new ArgumentNullException(nameof(center));
            }

            var queryBits = Math.Max(1, CalculateBoundingBoxBits(center, radiusMeters));
            var geohashPrecision = (int)Math.Ceiling((double)queryBits / BitsPerChar);
            if (geohashPrecision < 1 || geohashPrecision > 20)
            {
                geohashPrecision = DefaultGeoHashPrecision;
            }

            _log.Debug($"Calculated necessary hash precision to be {geohashPrecision} characters for the given distance");

            var coordinates = CalculateBoundingBoxCoordinates(center, radiusMeters);

            if (_log.IsDebugEnabled)
            {
                _log.Debug("********* Bounding Box Coordinates *********");
                foreach (var coordinate in coordinates)
                {
                    _log.Debug($"{coordinate.Latitude}, {coordinate.Longitude}");
                }
                _log.Debug("******************************************");
            }

            return
                coordinates.Select(geographyPoint => Encode(geographyPoint.Latitude, geographyPoint.Longitude, geohashPrecision))
                    .Select(hash => CreateGeohashQuery(hash, queryBits))
                    .Distinct(new StringArrayEqualityComparer());
        }

        private double CalculateLatitudeBitsForResolution(double resolutionMeters)
        {
            return Math.Min(Math.Log(EarthMeridianCircumferenceMeters / 2 / resolutionMeters, 2), MaxBitsPrecision);
        }

        private double CalculateLongitudeBitsForResolution(double resolutionMeters, double latitude)
        {
            var degrees = MetersToLongitudeDegrees(resolutionMeters, latitude);
            return Math.Abs(degrees) > 0.000001 ? Math.Max(1, Math.Log(360.0 / degrees, 2)) : 1;
        }

        private string[] CreateGeohashQuery(string geohash, int bits)
        {
            if (string.IsNullOrEmpty(geohash))
            {
                throw new ArgumentNullException(nameof(geohash));
            }

            var precision = (int)Math.Ceiling((double)bits / BitsPerChar);

            if (geohash.Length < precision)
            {
                return new[] { geohash, $"{geohash}~" };
            }
            
            geohash = geohash.Substring(0, precision);
            var baseValue = geohash.Substring(0, geohash.Length - 1);
            int lastValue = Base32.IndexOf(geohash.ToCharArray().Last());
            int significantBits = bits - (baseValue.Length * BitsPerChar);
            int unusedBits = BitsPerChar - significantBits;
            int startValue = (lastValue >> unusedBits) << unusedBits;
            int endValue = startValue + (1 << unusedBits);
            string start;
            string end;
            if (endValue > 31)
            {
                start = $"{baseValue}{Base32[startValue]}";
                end = $"{baseValue}~";
            }
            else
            {
                start = $"{baseValue}{Base32[startValue]}";
                end = $"{baseValue}{Base32[endValue]}";
            }
            
            return new[] { start, end };
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        /// <summary>
        /// Encodes a latitude, longitude point to a geohash string
        /// </summary>
        /// <param name="latitude">
        /// Latitude of point
        /// </param>
        /// <param name="longitude">
        /// Longitude of point
        /// </param>
        /// <param name="precision">
        /// precision resolution
        /// </param>
        /// <returns>
        /// An encoded geohash string
        /// </returns>
        /// <remarks>
        /// GeoHash Encode Function adapted from https://gist.github.com/KamChanLiu/16e27ebd77586c236289#file-geohashencode-cs
        /// </remarks>
        private string Encode(double latitude, double longitude, int precision = DefaultGeoHashPrecision)
        {
            var geohash = string.Empty;
            int bits = 0, totalBits = 0, hashValue = 0;
            double maxLat = 90, minLat = -90, maxLon = 180, minLon = -180;

            while (geohash.Length < precision)
            {
                double mid;
                if (totalBits % 2 == 0)
                {
                    mid = (maxLon + minLon) / 2;

                    if (longitude > mid)
                    {
                        hashValue = (hashValue << 1) + 1;
                        minLon = mid;
                    }
                    else
                    {
                        hashValue = (hashValue << 1) + 0;
                        maxLon = mid;
                    }
                }
                else
                {
                    mid = (maxLat + minLat) / 2;
                    if (latitude > mid)
                    {
                        hashValue = (hashValue << 1) + 1;
                        minLat = mid;
                    }
                    else
                    {
                        hashValue = (hashValue << 1) + 0;
                        maxLat = mid;
                    }
                }

                bits++;
                totalBits++;

                if (bits == 5)
                {
                    var code = Base32[hashValue];
                    geohash += code;
                    bits = 0;
                    hashValue = 0;
                }
            }

            return geohash;
        }

        private double MetersToLongitudeDegrees(double meters, double latitude)
        {
            var radians = DegreesToRadians(latitude);
            var numerator = Math.Cos(radians) * EarthEquitorialRadius * Math.PI / 180;
            var denominator = Math.Sqrt(1 - E2 * Math.Pow(Math.Sin(radians), 2));
            var deltaDegrees = numerator / denominator;
            if (deltaDegrees < Epsilon)
            {
                return meters > 0 ? 360 : 0;
            }

            return Math.Min(360, meters / deltaDegrees);
        }

        private double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        private void UpdateDistanceToCenter(GeographyPoint center, LocationSearchResult searchResult)
        {
            // approximate distance to center using haversine formula
            double dLat = ToRadians(center.Latitude - searchResult.Location.Latitude);
            double dLon = ToRadians(center.Longitude - searchResult.Location.Longitude);

            double a = Math.Pow(Math.Sin(dLat / 2), 2)
                       + (Math.Cos(ToRadians(center.Latitude)) * Math.Cos(ToRadians(searchResult.Location.Latitude)) * Math.Pow(Math.Sin(dLon / 2), 2));

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            searchResult.DistanceToCenterKm = (EarthEquitorialRadius / 1000) * c;
        }

        private double WrapLongitude(double longitude)
        {
            if (longitude <= 180 && longitude >= -180)
            {
                return longitude;
            }

            var adjusted = longitude + 180;

            if (adjusted > 0)
            {
                return (adjusted % 360) - 180;
            }

            return 180 - (-adjusted % 360);
        }

        #endregion
    }

    public class KeyValueKeyComparer<TKey, TValue> : IEqualityComparer<KeyValuePair<TKey, TValue>>
    {
        #region Fields

        private readonly IEqualityComparer<TKey> _comparer;

        #endregion

        #region Constructors and Destructors

        public KeyValueKeyComparer(IEqualityComparer<TKey> comparer)
        {
            _comparer = comparer;
        }

        #endregion

        #region Public Methods and Operators

        public bool Equals(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
        {
            return _comparer.Equals(x.Key, y.Key);
        }

        public int GetHashCode(KeyValuePair<TKey, TValue> obj)
        {
            return _comparer.GetHashCode(obj.Key);
        }

        #endregion
    }
}