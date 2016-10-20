using System;

using Newtonsoft.Json;

namespace FireSharp.Security
{
    public class AccessToken
    {
        public AccessToken()
        {
            Issued = DateTimeOffset.Now;
        }

        public AccessToken(DateTimeOffset issued)
        {
            Issued = issued;
        }

        public DateTimeOffset Issued { get; set; }

        [JsonProperty("access_token")]
        public string Value { get; set; }
        
        [JsonProperty("expires_in")]
        public int ExpiresInSeconds { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        public bool IsExpired { get; set; }
    }
}