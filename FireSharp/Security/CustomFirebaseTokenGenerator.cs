using System;
using System.Collections.Generic;
using System.Linq;

using FireSharp.Extensions;

using JosePCL;
using JosePCL.Keys.Rsa;

using Newtonsoft.Json;

namespace FireSharp.Security
{
    public class CustomFirebaseTokenGenerator
    {
        #region Constants

        private const string Audience = "https://identitytoolkit.googleapis.com/google.identity.identitytoolkit.v1.IdentityToolkit";

        #endregion

        #region Fields

        private readonly string _serviceAccountEmailAddress;

        private readonly string _serviceAccountPrivateKey;

        #endregion

        #region Constructors and Destructors

        public CustomFirebaseTokenGenerator(GoogleCloudCredentials googleCloudCredentials)
        {
            if (googleCloudCredentials == null)
            {
                throw new ArgumentNullException(nameof(googleCloudCredentials));
            }

            if (string.IsNullOrEmpty(googleCloudCredentials.ClientEmail))
            {
                throw new ArgumentException("ClientEmail property cannot be null or empty", nameof(googleCloudCredentials));
            }

            if (string.IsNullOrEmpty(googleCloudCredentials.PrivateKey))
            {
                throw new ArgumentException("PrivateKey property cannot be null or empty", nameof(googleCloudCredentials));
            }

            _serviceAccountEmailAddress = googleCloudCredentials.ClientEmail;
            _serviceAccountPrivateKey = googleCloudCredentials.PrivateKey;
        }

        public CustomFirebaseTokenGenerator(string serviceAccountEmailAddress, string serviceAccountPrivateKey)
        {
            if (serviceAccountEmailAddress == null)
            {
                throw new ArgumentNullException(nameof(serviceAccountEmailAddress));
            }

            if (serviceAccountPrivateKey == null)
            {
                throw new ArgumentNullException(nameof(serviceAccountPrivateKey));
            }

            _serviceAccountEmailAddress = serviceAccountEmailAddress;
            _serviceAccountPrivateKey = serviceAccountPrivateKey;
        }

        #endregion

        #region Public Methods and Operators

        public string GenerateToken(string userIdentifier, int tokenTimeToLiveSeconds = 60, IEnumerable<KeyValuePair<string, object>> claims = null)
        {
            var issuedTime = DateTimeOffset.UtcNow;

            var payload = new Dictionary<string, object>
                              {
                                  { "alg", JwsAlgorithms.RS256 }, 
                                  { "iss", _serviceAccountEmailAddress }, 
                                  { "sub", _serviceAccountEmailAddress }, 
                                  { "aud", Audience }, 
                                  { "iat", issuedTime.ToUnixTimestampSeconds() }, 
                                  { "exp", issuedTime.ToUnixTimestampSeconds() + tokenTimeToLiveSeconds }, 
                                  { "uid", userIdentifier }
                              };

            if (claims != null)
            {
                payload["claims"] = claims.ToList();
            }

            var privateKey = PrivateKey.Load(_serviceAccountPrivateKey);

            var payloadString = JsonConvert.SerializeObject(payload);

            return Jwt.Encode(payloadString, JwsAlgorithms.RS256, privateKey);
        }

        #endregion
    }
}