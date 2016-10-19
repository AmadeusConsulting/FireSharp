using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FireSharp.Security
{
    public class AuthSecretAuthenticator : IRequestAuthenticator
    {
        private readonly string _authSecret;

        public AuthSecretAuthenticator(string authSecret)
        {
            if (string.IsNullOrEmpty(authSecret))
            {
                throw new ArgumentNullException(nameof(authSecret));
            }

            _authSecret = authSecret;
        }

        public Task AddAuthentication(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.RequestUri.IsAbsoluteUri)
            {
                var builder = new UriBuilder(request.RequestUri);

                builder.Query = $"{builder.Query}{(string.IsNullOrEmpty(builder.Query) ? "?" : "&")}auth={_authSecret}";

                request.RequestUri = builder.Uri;
            }
            else
            {
                var pathAndQuery = request.RequestUri.ToString();
                var componentList = pathAndQuery.Split('?');
                var query = componentList.Length > 1 ? componentList[1] : string.Empty;
                var path = componentList[0];

                request.RequestUri = new Uri(
                                         $"{path}?{query}{(string.IsNullOrEmpty(query) ? string.Empty : "&")}auth={_authSecret}",
                                         UriKind.Relative);
            }
            return Task.FromResult(0);
        }
    }
}