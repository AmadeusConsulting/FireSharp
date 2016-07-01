using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FireSharp.Tests.Mocks
{
    public class MockableHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                                      {
                                          Content = new StringContent("Mock me, I dare you.")
                                      };
            return Task.FromResult(responseMessage);
        }
    }
}
