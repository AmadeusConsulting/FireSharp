using System;

using Newtonsoft.Json;

namespace FireSharp.Security
{
    public class IdentityToken
    {
        public IdentityToken()
        {
            Issued = DateTimeOffset.Now;
        }

        public IdentityToken(DateTimeOffset issued)
        {
            Issued = issued;
        }

        [JsonIgnore]
        public DateTimeOffset Issued { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("idToken")]
        public string Value { get; set; }

        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; }

        [JsonProperty("expiresIn")]
        public int ExpiresInSeconds { get; set; }

        public bool IsExpired => Issued.AddSeconds(ExpiresInSeconds) < DateTimeOffset.Now;
    }
}