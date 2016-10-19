using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Common.Testing.NUnit;

using FireSharp.Security;

using NUnit.Framework;

namespace FireSharp.Tests
{
    public class RequestAuthenticatorTests : TestBase
    {
        [Test]
        public void AuthSecretAuthenticatorAddsQueryParameter()
        {
            var authenticator = new AuthSecretAuthenticator("TEST_TOKEN_VALUE");

            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("todos/requestAuth.json", UriKind.Relative));

            authenticator.AddAuthentication(request);

            Assert.AreEqual("todos/requestAuth.json?auth=TEST_TOKEN_VALUE", request.RequestUri.ToString());
        }

        [Test]
        public void AuthSecretAuthenticatorAddsQueryParameterToExistingParameters()
        {
            var authenticator = new AuthSecretAuthenticator("TEST_TOKEN_VALUE");

            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("todos/requestAuth.json?foo=bar", UriKind.Relative));

            authenticator.AddAuthentication(request);

            Assert.AreEqual("todos/requestAuth.json?foo=bar&auth=TEST_TOKEN_VALUE", request.RequestUri.ToString());
        }
    }
}
