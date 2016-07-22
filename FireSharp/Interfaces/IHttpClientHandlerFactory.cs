using System.Net.Http;

namespace FireSharp.Interfaces
{
    public interface IHttpClientHandlerFactory
    {
        HttpMessageHandler CreateHandler(bool allowAutoRedirects = false);
    }
}