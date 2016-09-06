using System.Collections.Generic;

namespace FireSharp.Security
{
    public interface IFirebaseCustomTokenGenerator
    {
        string GenerateToken(string userIdentifier, int tokenTimeToLiveSeconds = 60, IDictionary<string, object> claims = null, bool debug = false);
    }
}