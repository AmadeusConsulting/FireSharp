using System.Collections.Generic;

using Newtonsoft.Json;

namespace FireSharp.GeoFire
{
    public class GeoHashLocation
    {
        public const string GeoHashPropertyName = "g";

        public const string LocationPropertyName = "l";

        private System.Collections.Generic.IDictionary<string, string> _metadata;

        [JsonProperty(PropertyName = GeoHashPropertyName)]
        public string GeoHash { get; set; }

        [JsonIgnore]
        public GeographyPoint Location { get; set; }

        [JsonProperty(PropertyName = LocationPropertyName)]
        public double[] LocationValue
        {
            get
            {
                return Location != null ? new[] { Location.Latitude, Location.Longitude } : null;
            }
            set
            {
                if (value != null)
                {
                    Location = new GeographyPoint
                                   {
                                       Latitude = value[0],
                                       Longitude = value[1]
                                   };
                }
            }
        }

        public System.Collections.Generic.IDictionary<string, string> Metadata
        {
            get
            {
                return _metadata ?? (_metadata = new Dictionary<string, string>());
            }
            set
            {
                _metadata = value;
            }
        }
    }
}