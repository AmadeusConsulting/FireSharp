using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;

using Common.Testing.NUnit;

using FireSharp.Config;
using FireSharp.Interfaces;

using Newtonsoft.Json;

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

        protected string UniquePathId;

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

                UniquePathId = SetUpUniqueFirebaseUrlPath();
                FirebaseUrlWithoutSlash = FirebaseUrl.Substring(0, FirebaseUrl.Length - 1);

                Config = new FirebaseConfig
                {
                    AuthSecret = FirebaseSecret,
                    BasePath = FirebaseUrl
                };

                FirebaseClient = new FirebaseClient(Config); //Uses Newtonsoft.Json Json Serializer 
            }
        }

        protected virtual string SetUpUniqueFirebaseUrlPath()
        {
            if (!string.IsNullOrEmpty(FirebaseUrl))
            {
                var uniqueId = Guid.NewGuid().ToString("N");

                FirebaseUrl = $"{FirebaseUrl}{uniqueId}/";

                return uniqueId;
            }
            return string.Empty;
        }
    }
}