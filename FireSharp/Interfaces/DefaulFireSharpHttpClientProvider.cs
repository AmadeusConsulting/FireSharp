using System;
using System.Collections.Generic;
using System.Net.Http;

namespace FireSharp.Interfaces
{
    public class DefaulFireSharpHttpClientProvider : IHttpClientProvider
    {
        #region Fields

        private readonly IDictionary<string, HttpClient> _httpClientTable = new Dictionary<string, HttpClient>();

        #endregion

        #region Public Methods and Operators

        public HttpClient GetHttpClient(Uri basePath, TimeSpan? requestTimeout = null)
        {
            if (!_httpClientTable.ContainsKey(basePath.ToString()))
            {
                var handler = new HttpClientHandler
                                  {
                                      AllowAutoRedirect = true
                                  };

                var client = new HttpClient(handler, false)
                                 {
                                     BaseAddress = basePath
                                 };

                if (requestTimeout.HasValue)
                {
                    client.Timeout = requestTimeout.Value;
                }

                _httpClientTable[basePath.ToString()] = client;
            }

            return _httpClientTable[basePath.ToString()];
        }

        #endregion
    }
}