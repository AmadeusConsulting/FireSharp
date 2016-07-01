using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Common.Testing.NUnit;

using FireSharp.Config;
using FireSharp.Interfaces;
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

        private Mock<IHttpClientHandlerFactory> _handlerFactory;

        protected override void FinalizeSetUp()
        {
            _mockHttpHandler = MockFor<MockableHttpMessageHandler>();
            _handlerFactory = MockFor<IHttpClientHandlerFactory>();

            _handlerFactory.Setup(hf => hf.CreateHandler(It.IsAny<bool>())).Returns(_mockHttpHandler.Object);

            _requestManager = new RequestManager(
                new FirebaseConfig
                    {
                        BasePath = "http://not-a-valid-firebase.url",
                        HttpClientHandlerFactory = _handlerFactory.Object
                    });
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
