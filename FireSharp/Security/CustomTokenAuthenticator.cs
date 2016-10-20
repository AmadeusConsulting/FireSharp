using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using FireSharp.Exceptions;
using FireSharp.Interfaces;
using FireSharp.Logging;

namespace FireSharp.Security
{
    public class CustomTokenAuthenticator : IRequestAuthenticator
    {
        #region Constants

        private const string IdentityToolkitTokenServiceUrl = "https://securetoken.googleapis.com/v1/";

        private const string IdentityToolkitUrl = "https://www.googleapis.com/identitytoolkit/v3/";

        #endregion

        #region Fields

        private readonly string _customToken;

        private readonly string _googleApiKey;

        private readonly IHttpClientProvider _httpClientProvider;

        private readonly ISerializer _serializer;

        private readonly object _tokenSync = new object();

        private ILog _log;

        #endregion

        #region Constructors and Destructors

        public CustomTokenAuthenticator(
            string googleApiKey,
            string customToken,
            IHttpClientProvider httpClientProvider,
            ISerializer serializer,
            ILogManager logManager)
        {
            if (googleApiKey == null)
            {
                throw new ArgumentNullException(nameof(googleApiKey));
            }
            if (customToken == null)
            {
                throw new ArgumentNullException(nameof(customToken));
            }
            if (httpClientProvider == null)
            {
                throw new ArgumentNullException(nameof(httpClientProvider));
            }
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }
            if (logManager == null)
            {
                throw new ArgumentNullException(nameof(logManager));
            }

            _log = logManager.GetLogger(this);
            _googleApiKey = googleApiKey;
            _customToken = customToken;
            _httpClientProvider = httpClientProvider;
            _serializer = serializer;

            TokenUpdateTask = ObtainAccessToken(customToken);
        }

        #endregion

        #region Public Properties

        public IdentityToken IdentityToken { get; private set; }

        public Task TokenUpdateTask { get; private set; }

        #endregion

        #region Public Methods and Operators

        public async Task AddAuthentication(HttpRequestMessage request)
        {
            await EnsureValidToken();

            if (request.RequestUri.IsAbsoluteUri)
            {
                var builder = new UriBuilder(request.RequestUri);

                builder.Query = $"{builder.Query}{(string.IsNullOrEmpty(builder.Query) ? "?" : "&")}auth={IdentityToken.Value}";

                request.RequestUri = builder.Uri;
            }
            else
            {
                var pathAndQuery = request.RequestUri.ToString();
                var componentList = pathAndQuery.Split('?');
                var query = componentList.Length > 1 ? componentList[1] : string.Empty;
                var path = componentList[0];

                request.RequestUri = new Uri(
                                         $"{path}?{query}{(string.IsNullOrEmpty(query) ? string.Empty : "&")}auth={IdentityToken.Value}",
                                         UriKind.Relative);
            }
        }

        public async Task RefreshIdToken()
        {
            var client = _httpClientProvider.GetHttpClient(new Uri(IdentityToolkitTokenServiceUrl));

            var requestContent = new StringContent(
                                     $"grant_type=refresh_token&refresh_token={IdentityToken.RefreshToken}",
                                     Encoding.UTF8,
                                     "application/x-www-form-urlencoded");

            var request = new HttpRequestMessage(HttpMethod.Post, $"token?key={_googleApiKey}")
                              {
                                  Content = requestContent,
                                  Headers =
                                      {
                                              { "Accept", "application/json" }
                                      }
                              };

            _log.Debug("Refreshing Identity Token ...");

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var message = $"Token refresh failed with HttpStatus {response.StatusCode}: {response.ReasonPhrase}";
                _log.Error($"{message}\n{await response.Content.ReadAsStringAsync()}");
                throw new FirebaseException(message);
            }

            var responseBodyJson = await response.Content.ReadAsStringAsync();

            _log.Debug($"Refresh request body:\n\n{responseBodyJson}");

            var accessToken =_serializer.Deserialize<AccessToken>(responseBodyJson);

            UpdateToken(accessToken);
        }

        #endregion

        #region Methods

        private async Task EnsureValidToken()
        {
            await TokenUpdateTask;
            if (!IdentityToken.IsExpired)
            {
                return;
            }

            TokenUpdateTask = RefreshIdToken();

            await TokenUpdateTask;
        }

        private async Task ObtainAccessToken(string customToken)
        {
            var client = _httpClientProvider.GetHttpClient(new Uri(IdentityToolkitUrl));

            var requestBody = new VerifyCustomTokenRequest
                                  {
                                      ReturnSecureToken = true,
                                      Token = customToken
                                  };

            var requestContent = new StringContent(_serializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"relyingparty/verifyCustomToken?key={_googleApiKey}")
                              {
                                  Content = requestContent,
                                  Headers =
                                      {
                                              { "Accept", "application/json" }
                                      }
                              };

            _log.Debug("Obtaining Identity Token ...");

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var message = $"Custom token verification failed with HttpStatus {response.StatusCode}: {response.ReasonPhrase}";
                _log.Error($"{message}\n{await response.Content.ReadAsStringAsync()}");
                throw new FirebaseException(message);
            }

            var responseBodyJson = await response.Content.ReadAsStringAsync();

            _log.Debug($"Token Response:\n\n{responseBodyJson}");

            IdentityToken = _serializer.Deserialize<IdentityToken>(responseBodyJson);
        }

        private void UpdateToken(AccessToken accessToken)
        {
            lock (_tokenSync)
            {
                IdentityToken.Value = accessToken.Value;
                IdentityToken.RefreshToken = accessToken.RefreshToken;
                IdentityToken.Issued = accessToken.Issued;
                IdentityToken.ExpiresInSeconds = accessToken.ExpiresInSeconds;
            }
        }

        #endregion
    }
}