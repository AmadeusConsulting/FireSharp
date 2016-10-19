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
using FireSharp.Security;
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

        private Mock<IHttpClientProvider> _httpClientProvider;

        private Uri _baseUri;

        private Mock<IRequestAuthenticator> _requestAuthenticator;

        protected override void FinalizeSetUp()
        {
            _serializer = new JsonNetSerializer();
            _mockHttpHandler = MockFor<MockableHttpMessageHandler>();
            _baseUri = new Uri("http://not-a-valid-firebase.url/");

            _httpClient = new HttpClient(_mockHttpHandler.Object, false)
                              {
                                  BaseAddress = _baseUri
                              };

            _httpClientProvider = MockFor<IHttpClientProvider>();

            _requestAuthenticator = MockFor<IRequestAuthenticator>();

            _requestAuthenticator.Setup(ra => ra.AddAuthentication(It.IsAny<HttpRequestMessage>())).Returns(Task.FromResult(0));

            _httpClientProvider.Setup(p => p.GetHttpClient(It.IsAny<Uri>(), It.IsAny<TimeSpan?>())).Returns(_httpClient);

            _requestManager = new RequestManager(_baseUri, _httpClientProvider.Object, _serializer, new NoOpLogManager(), _requestAuthenticator.Object);
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

            requestContent.ShouldBeEquivalentTo(@"{""name"":""foo bar"",""priority"":1,""assignee"":null}");
        }
    }
}
