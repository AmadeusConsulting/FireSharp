using System;
using System.Net.Http;

namespace FireSharp.Interfaces
{
    public interface IHttpClientProvider
    {
        HttpClient GetHttpClient(Uri basePath, TimeSpan? requestTimeout = null);
    }
}