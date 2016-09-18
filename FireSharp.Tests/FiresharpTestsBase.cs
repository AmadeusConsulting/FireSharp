using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;

using Common.Testing.NUnit;

using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Tests.Logging;

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
                
                Config = new FirebaseConfig
                {
                    AuthSecret = FirebaseSecret,
                    BasePath = $"{FirebaseUrl}{UniquePathId}/",
                    LogManager = new DebugLogManager()
                };

                FirebaseUrlWithoutSlash = Config.BasePath.Substring(0, Config.BasePath.Length - 1);

                FirebaseClient = new FirebaseClient(Config); //Uses Newtonsoft.Json Json Serializer 

                SetupFirebaseRules();
            }
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            TearDownFirebaseRules();
        }

        protected virtual void SetupFirebaseRules()
        {
            
        }

        protected virtual void TearDownFirebaseRules()
        {
        }

        protected virtual string SetUpUniqueFirebaseUrlPath()
        {
            if (!string.IsNullOrEmpty(FirebaseUrl))
            {
                var uniqueId = Guid.NewGuid().ToString("N");
                
                return uniqueId;
            }
            return string.Empty;
        }
    }
}