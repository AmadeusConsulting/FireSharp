using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Common.Testing.NUnit;

using FireSharp.Config;
using FireSharp.Security;

using JosePCL;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NUnit.Framework;

namespace FireSharp.Tests
{
    public class CustomTokenTests : TestBase
    {
        private string _serviceAccountJsonFilePath;

        private string _googleApiKey;

        private const string PrivateKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEA0Ki6oahn30WLnX96HShUFzd6EjHrFv4W2AC8FsVG9eVqXNWG
x1FEwaSO1OQ4N+jO7YHL4z0KS3l45LQMh4Yav3gJG8KrPKfMDE5DujAVYA1uW5vV
k1nW+UhKs8fU6ksD4T2wb9STXMdhsuqYlPf7QZg8FewuBimcqY5+3eJ1KRKEs3o6
xEfVMjspobRYxbH89o0x5fqQqvT6+KFHZ7hqtmTBWjD07qUP6eEE6nGdCHxBPWrq
bxPbVoir4sn2XHUjurar0iO2hFTx0ny83Yy6nml6IxDNUFXUW4HUavLt/E3LRJgn
UcACODlybIoQqlrPh/iuao89wfki7vk+6Dhn+wIDAQABAoIBAFZE7KTZ09hkEI1V
n09e1SWkMjxDl0cyVo+H/jwL5ILWD1vCjK7r0tmdB9DNatDy2FsqRvn0ysTZvDoS
lvuRXo27O0jW+6VgWuTblvTr3GqSIwaNL4h0pIT8dqZOAKG90iTWNJSH90VsyeAH
Tn6I6MD+CBmoRH8XxUX1PE7Qyk/oMP7qVyOVKgwXeE97AlAFM/DhHUK3bznlVfOo
WVCUb6xkUnlS1zE08+bQbR/fDaRIqr91yBzvtjuMpiSlz3OuOPoxbLYuQ1EMeV5c
hehYrJyj0n73rp00Ex+h388AyotXnWK/AuTVeSceF7o2u9+EcL0s/srz8R3v+uH+
NyGCykECgYEA66Evs1wQwyOIuw0fzG8JGPipOcXb3KcaS2M/BMSqJwuCXZcCkgvc
/fDS1uRjRxg7mdkNvLAjx44xJxY4ObUKIhlqm4to9coWZZU//HqG3n4QJXhMQJ+n
DyQi7T1obI27Ms+XzfyWz8PrhXWozT1OQCOELojP/pLVC9DTMTYCZe8CgYEA4rKl
n/3A4YVsedopNAdgq9oEL2qGYoYdzhwWyAhIYqYVuKoc6/SUeWkasHowEbsiM/m2
EYcVYqkZTEAskWgGloZd6VrOd0RpiCGEqpKnVYnIhYsAld16Y2Bw0k8EyNGkRm1T
b+VLYTJnLmqcukJXdefXBb1w+tbZe2Xd3GslCrUCgYEAmsjAl/bzf+yYxh9B8z5B
YKkAKVZjLliK+ljQreYuzAVQdwBbDOtEGYCYT9epq6ssg8zErF2cs1shyMZc2vOl
G29My67RnRxKiCJ57PXkIMX4/1Q96vm1eUnIs8VyElUsUp1x4Dt8KjFORtZas7AA
9jseep8e2uFDmEZuZVZQTvsCgYAyp6CCo3zZ56pZf/n8+jkLrWeKAM+ObFF0oKom
gFNMV7g6zygvQTN7/ZRNIsBi9eGqo32fZQPOS+KvOxe6VfhC4jtRzUydMdgy5upy
AtsJLgR0cp7q3dZfJkmPdMCo7s86PWuLcTzqtwx/PqtOo0xPuEI/shjws9lczWJJ
wldAkQKBgHqA0bl/Hi30irJ9/2JeOlys2RJhZ3wPRNFYOHtQt19Os52wPiZ2xu9h
wMZtUj686tdhBYqLnyzesnjIFSzuCcNcfeglZ1ow5RNwiNWw/DfgtVl8ZPLci04z
PgCMivf/Y6A5PWlPrKhGbCKEwwN1wFInjOsyXAp2Qx1gCNuRBGjw
-----END RSA PRIVATE KEY-----";
        
        private const string PublicKey = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA0Ki6oahn30WLnX96HShU
Fzd6EjHrFv4W2AC8FsVG9eVqXNWGx1FEwaSO1OQ4N+jO7YHL4z0KS3l45LQMh4Ya
v3gJG8KrPKfMDE5DujAVYA1uW5vVk1nW+UhKs8fU6ksD4T2wb9STXMdhsuqYlPf7
QZg8FewuBimcqY5+3eJ1KRKEs3o6xEfVMjspobRYxbH89o0x5fqQqvT6+KFHZ7hq
tmTBWjD07qUP6eEE6nGdCHxBPWrqbxPbVoir4sn2XHUjurar0iO2hFTx0ny83Yy6
nml6IxDNUFXUW4HUavLt/E3LRJgnUcACODlybIoQqlrPh/iuao89wfki7vk+6Dhn
+wIDAQAB
-----END PUBLIC KEY-----";

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            _serviceAccountJsonFilePath = ConfigurationManager.AppSettings["FireSharp.Tests.GoogleServiceAccount.JsonFilePath"];
            _googleApiKey = ConfigurationManager.AppSettings["FireSharp.Tests.GoogleApiKey"];
        }


        [Test]
        public void GenerateCustomToken()
        {
            var serviceAccountEmailAddress = "a-fake@invalid.eml";
            var tokenGenerator = new FirebaseCustomTokenGenerator(serviceAccountEmailAddress, PrivateKey);

            var token = tokenGenerator.GenerateToken(
                "123",
                claims: new Dictionary<string, object>
                            {
                                { "test-claim-1", true },
                                { "test-claim-2", "a value" },
                                { "test-claim-3", 100 }
                            });

            Assert.IsNotNullOrEmpty(token);

            var json = Jwt.Decode(token, JosePCL.Keys.Rsa.PublicKey.Load(PublicKey));

            var payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            Assert.AreEqual(serviceAccountEmailAddress, payload["iss"]);
            Assert.AreEqual(serviceAccountEmailAddress, payload["sub"]);
            Assert.AreEqual("https://identitytoolkit.googleapis.com/google.identity.identitytoolkit.v1.IdentityToolkit", payload["aud"]);
            Assert.AreEqual("123", payload["uid"]);
        }

        [Test, Category("INTEGRATION")]
        public async void GenerateValidToken()
        {
            if (string.IsNullOrEmpty(_serviceAccountJsonFilePath) || string.IsNullOrEmpty(_googleApiKey))
            {
                Assert.Inconclusive();
            }

            var googleCredentialsJson = File.ReadAllText(_serviceAccountJsonFilePath);
            var googleApiKey = _googleApiKey;

            var googleCredentials = JsonConvert.DeserializeObject<GoogleCloudCredentials>(googleCredentialsJson);
            var tokenGenerator = new FirebaseCustomTokenGenerator(googleCredentials, new FirebaseConfig());

            var token = tokenGenerator.GenerateToken("1234", debug: true);

            Debug.WriteLine("Token NO Claims:");
            Debug.WriteLine(token);

            var client = new HttpClient();

            var requestJson = new JObject
                                  {
                                      ["returnSecureToken"] = new JValue(true),
                                      ["token"] = new JValue(token)
                                  };

            var requestContent = new StringContent(JsonConvert.SerializeObject(requestJson), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://www.googleapis.com/identitytoolkit/v3/relyingparty/verifyCustomToken?key={googleApiKey}")
                              {
                                  Content = requestContent,
                                  Headers =
                                      {
                                          { "Accept", "application/json" }
                                      }
                              };

            var response = await client.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseBodyJson = await response.Content.ReadAsStringAsync();

            var responseValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBodyJson);

            Assert.IsTrue(responseValues.ContainsKey("idToken"));
            Assert.IsNotNullOrEmpty(responseValues["idToken"]);

            Assert.AreEqual("identitytoolkit#VerifyCustomTokenResponse", responseValues["kind"]);
        }

        [Test, Category("INTEGRATION")]
        public async void GenerateValidTokenWithCustomClaims()
        {
            if (string.IsNullOrEmpty(_serviceAccountJsonFilePath) || string.IsNullOrEmpty(_googleApiKey))
            {
                Assert.Inconclusive();
            }

            var googleCredentialsJson = File.ReadAllText(_serviceAccountJsonFilePath);
            var googleApiKey = _googleApiKey;

            var googleCredentials = JsonConvert.DeserializeObject<GoogleCloudCredentials>(googleCredentialsJson);
            var tokenGenerator = new FirebaseCustomTokenGenerator(googleCredentials, new FirebaseConfig());

            var token = tokenGenerator.GenerateToken(
                "1234",
                debug: true,
                claims: new Dictionary<string, object>
                            {
                                ["tester"] = "yes"
                            });

            Debug.WriteLine("Token Custom Claims:");
            Debug.WriteLine(token);

            var client = new HttpClient();

            var requestJson = new JObject
            {
                ["returnSecureToken"] = new JValue(true),
                ["token"] = new JValue(token)
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(requestJson), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://www.googleapis.com/identitytoolkit/v3/relyingparty/verifyCustomToken?key={googleApiKey}")
            {
                Content = requestContent,
                Headers =
                                      {
                                          { "Accept", "application/json" }
                                      }
            };

            var response = await client.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseBodyJson = await response.Content.ReadAsStringAsync();

            var responseValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBodyJson);

            Assert.IsTrue(responseValues.ContainsKey("idToken"));
            Assert.IsNotNullOrEmpty(responseValues["idToken"]);

            Assert.AreEqual("identitytoolkit#VerifyCustomTokenResponse", responseValues["kind"]);
        }

        [Test, Category("INTEGRATION")]
        public async void CreateValidTokenWithEmptyClaims()
        {
            if (string.IsNullOrEmpty(_serviceAccountJsonFilePath) || string.IsNullOrEmpty(_googleApiKey))
            {
                Assert.Inconclusive();
            }

            var googleCredentialsJson = File.ReadAllText(_serviceAccountJsonFilePath);
            var googleApiKey = _googleApiKey;

            var googleCredentials = JsonConvert.DeserializeObject<GoogleCloudCredentials>(googleCredentialsJson);
            var tokenGenerator = new FirebaseCustomTokenGenerator(googleCredentials, new FirebaseConfig());

            var token = tokenGenerator.GenerateToken("1234", debug: true, claims: new Dictionary<string, object>());

            Debug.WriteLine("Token Empty Claims:");
            Debug.WriteLine(token);

            var client = new HttpClient();

            var requestJson = new JObject
            {
                ["returnSecureToken"] = new JValue(true),
                ["token"] = new JValue(token)
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(requestJson), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://www.googleapis.com/identitytoolkit/v3/relyingparty/verifyCustomToken?key={googleApiKey}")
            {
                Content = requestContent,
                Headers =
                                      {
                                          { "Accept", "application/json" }
                                      }
            };

            var response = await client.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseBodyJson = await response.Content.ReadAsStringAsync();

            var responseValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBodyJson);

            Assert.IsTrue(responseValues.ContainsKey("idToken"));
            Assert.IsNotNullOrEmpty(responseValues["idToken"]);

            Assert.AreEqual("identitytoolkit#VerifyCustomTokenResponse", responseValues["kind"]);
        }
    }
}
