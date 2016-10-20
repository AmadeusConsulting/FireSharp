using System;
using System.Collections.Generic;
using System.Linq;

using FireSharp.Extensions;
using FireSharp.Interfaces;
using FireSharp.Logging;

using JosePCL;
using JosePCL.Keys.Rsa;

using Newtonsoft.Json;

namespace FireSharp.Security
{
    public class FirebaseCustomTokenGenerator : IFirebaseCustomTokenGenerator
    {
        #region Constants

        private const string Audience = "https://identitytoolkit.googleapis.com/google.identity.identitytoolkit.v1.IdentityToolkit";

        #endregion

        #region Fields

        private readonly string _serviceAccountEmailAddress;

        private readonly string _serviceAccountPrivateKey;

        private ILog _log;

        #endregion

        #region Constructors and Destructors

        public FirebaseCustomTokenGenerator(GoogleCloudCredentials googleCloudCredentials, ILogManager logManager)
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

            _log = logManager.GetLogger(this);
            _serviceAccountEmailAddress = googleCloudCredentials.ClientEmail;
            _serviceAccountPrivateKey = googleCloudCredentials.PrivateKey;
        }

        public FirebaseCustomTokenGenerator(string serviceAccountEmailAddress, string serviceAccountPrivateKey)
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
            _log = new NoOpLogger();
        }

        #endregion

        #region Public Methods and Operators

        public string GenerateToken(string userIdentifier, int tokenTimeToLiveSeconds = 60, IDictionary<string, object> claims = null, bool debug = false)
        {
            var issuedTime = DateTimeOffset.UtcNow;


            var payload = new Dictionary<string, object>
                              {
                                  { "iss", _serviceAccountEmailAddress }, 
                                  { "sub", _serviceAccountEmailAddress }, 
                                  { "aud", Audience }, 
                                  { "iat", issuedTime.ToUnixTimestampSeconds() }, 
                                  { "exp", issuedTime.ToUnixTimestampSeconds() + tokenTimeToLiveSeconds }, 
                                  { "uid", userIdentifier }
                              };

            if (claims != null && claims.Any())
            {
                payload["claims"] = claims;
            }

            if (debug)
            {
                if (!payload.ContainsKey("claims") || payload["claims"] == null)
                {
                    payload["claims"] = new Dictionary<string,object>();
                }
                payload["debug"] = true;
            }
            
            var privateKey = PrivateKey.Load(_serviceAccountPrivateKey);

            var payloadString = JsonConvert.SerializeObject(payload);

            _log.Debug($"Generating Firebase Token with the following payload:\n{payloadString}");

            return Jwt.Encode(payloadString, JwsAlgorithms.RS256, privateKey);
        }

        #endregion
    }
}