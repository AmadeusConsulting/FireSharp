using System.Net.Http;

using FireSharp.Interfaces;

namespace FireSharp
{
    internal class DefaultHttpClientHandlerFactory : IHttpClientHandlerFactory
    {
        public HttpMessageHandler CreateHandler(bool allowAutoRedirects = true)
        {
            return new HttpClientHandler
                       {
                           AllowAutoRedirect = allowAutoRedirects
                       };
        }
    }
}