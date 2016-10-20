using Newtonsoft.Json;

namespace FireSharp.Security
{
    public class VerifyCustomTokenRequest
    {
        [JsonProperty("returnSecureToken")]
        public bool ReturnSecureToken { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }
}