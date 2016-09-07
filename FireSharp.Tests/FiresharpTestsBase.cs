using System;
using System.Configuration;

using Common.Testing.NUnit;

using FireSharp.Config;
using FireSharp.Interfaces;

using NUnit.Framework;

namespace FireSharp.Tests
{
    public abstract class FiresharpTestsBase : TestBase
    {
        protected string FirebaseUrl;

        protected string FirebaseUrlWithoutSlash;

        protected IFirebaseClient FirebaseClient;

        protected IFirebaseConfig Config;

        protected string FirebaseSecret;

        [TestFixtureSetUp]
        public virtual void TestFixtureSetUp()
        {
            FirebaseUrl = ConfigurationManager.AppSettings["FireSharp.Tests.FirebaseUrl"];
            FirebaseSecret = ConfigurationManager.AppSettings["FireSharp.Tests.FirebaseSecret"];

            if (!string.IsNullOrEmpty(FirebaseUrl) && !string.IsNullOrEmpty(FirebaseSecret))
            {
                if (!FirebaseUrl.EndsWith("/"))
                {
                    FirebaseUrl = $"{FirebaseUrl}/";
                }

                SetUpUniqueFirebaseUrlPath();
                FirebaseUrlWithoutSlash = FirebaseUrl.Substring(0, FirebaseUrl.Length - 1);

                Config = new FirebaseConfig
                {
                    AuthSecret = FirebaseSecret,
                    BasePath = FirebaseUrl
                };

                FirebaseClient = new FirebaseClient(Config); //Uses Newtonsoft.Json Json Serializer 
            }
        }

        protected virtual void SetUpUniqueFirebaseUrlPath()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            FirebaseUrl = $"{FirebaseUrl}{uniqueId}/";
        }
    }
}