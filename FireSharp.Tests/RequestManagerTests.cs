using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Common.Testing.NUnit;

using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Logging;
using FireSharp.Tests.Mocks;
using FireSharp.Tests.Models;

using FluentAssertions;

using Moq;
using Moq.Protected;

using NUnit.Framework;

namespace FireSharp.Tests
{
    public class RequestManagerTests : TestBase
    {
        private RequestManager _requestManager;

        private Mock<MockableHttpMessageHandler> _mockHttpHandler;

        private HttpClient _httpClient;

        private ISerializer _serializer;

        protected override void FinalizeSetUp()
        {
            _serializer = new JsonNetSerializer();
            _mockHttpHandler = MockFor<MockableHttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpHandler.Object, false)
                              {
                                  BaseAddress = new Uri("http://not-a-valid-firebase.url/")
                              };
            
            _requestManager = new RequestManager(_httpClient, _serializer, new NoOpLogManager());
        }

        [Test]
        public void JsonIgnoreIsRespected()
        {
            string requestContent = string.Empty;

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)))
                .Callback<HttpRequestMessage, CancellationToken>(
                    (msg, token) =>
                        {
                            requestContent = msg.Content.ReadAsStringAsync().Result;
                        });

            _requestManager.RequestAsync(
                HttpMethod.Get,
                "some/path",
                new Todo
                    {
                        name = "foo bar",
                        priority = 1,
                        notSerialized = "This doesn't belong in JSON"
                    });

            requestContent.ShouldBeEquivalentTo(@"{""name"":""foo bar"",""priority"":1}");
        }
    }
}
