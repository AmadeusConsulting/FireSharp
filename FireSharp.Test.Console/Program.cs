using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

using FireSharp.Config;
using FireSharp.EventStreaming;
using FireSharp.Interfaces;
using FireSharp.Security;
using FireSharp.Tests.Logging;

using log4net;
using log4net.Config;

using Newtonsoft.Json;

namespace FireSharp.Test.Console
{
    public class Program
    {
        private static string sBasePath = ConfigurationManager.AppSettings["Firebase.DatabaseUrl"];
        private static string sFirebaseSecret = ConfigurationManager.AppSettings["Firebase.DatabaseSecret"];
        private static FirebaseClient _client;

        private static void Main()
        {
            XmlConfigurator.Configure();

            LogManager.GetLogger(typeof(Program)).Info("FireSharp Test Harness Starting....");

            IFirebaseConfig config = new FirebaseConfig
                                         {
                                             AuthSecret = sFirebaseSecret,
                                             BasePath = sBasePath,
                                             LogManager = new Log4NetLogManager()
                                         };

            _client = new FirebaseClient(config); //Uses JsonNet default
            
            //EntityEventStreaming();
            //PersonGenerator();
            //EventStreaming();
            //Crud();

            GenerateToken(config);

            System.Console.Read();
        }

        private static void GenerateToken(IFirebaseConfig config)
        {
            string serviceAccountJson = @"E:\Projects\towcentral\service-account.json";

            var googleCredentials = JsonConvert.DeserializeObject<GoogleCloudCredentials>(File.ReadAllText(serviceAccountJson));

            var generator = new FirebaseCustomTokenGenerator(googleCredentials, config);

            var token = generator.GenerateToken("111", 3600, debug: true);

            System.Console.WriteLine($"Token: \n{token}");

            System.Console.ReadKey();
        }
        
        private static async void PersonGenerator()
        {
            var destinations = new[] { "The Mall", "Home", "School", "Work", "Grocery Store" };

            for (int i = 0; i < 100; i++)
            {
                var pushId = PushId.NewId();
                var person = new Person
                                 {
                                     Name = $"Person No. {i}",
                                     FirebaseKey = pushId,
                                     Destination = destinations[i % 5],
                                     LatitudeLongitude = new List<double>
                                                             {
                                                                 36.999084,
                                                                 -109.045224
                                                             }
                                 };
                
                await _client.SetAsync($"persons/{pushId}", person);
            }
        }

        private static async void Crud()
        {
            var setResponse = await _client.SetAsync("todos", new {name = "SET CALL"});
            System.Console.WriteLine(setResponse.Body);
        }

        private static async void EntityEventStreaming()
        {
            await _client.MonitorEntityListAsync(
                "persons",
                ((sender, key, entity) =>
                    {
                        if (entity != null)
                        {
                            System.Console.WriteLine($"---- ADDED ----\n {key} \n----\n {entity} \n----\n");
                        }
                    }),
                (sender, key, newValue, oldValue) =>
                    {
                        System.Console.WriteLine(
                            $"---- CHANGED ----\n {key} \n Old Value \n ---- \n {oldValue} \n ---- New Value \n ---- \n {newValue}");
                    },
                (sender, key, removed) =>
                    {
                        System.Console.WriteLine($"---- REMOVED ----\n {key} \n----\n {removed} \n----\n");
                    },
                new InMemoryEntityResponseCache<Person>(),
                QueryBuilder.New().OrderBy("dest").EqualTo("Home")).ConfigureAwait(false);
        }

        private static async void EventStreaming()
        {
            await _client.DeleteAsync("chat");

            await _client.OnAsync("chat",
                async (sender, args, context) =>
                {
                    System.Console.WriteLine(args.Data + "-> 1\n");
                    await _client.PushAsync("chat/", new
                    {
                        name = "someone",
                        text = "Console 1:" + DateTime.Now.ToString("f")
                    });
                },
                (sender, args, context) => { System.Console.WriteLine(args.Data); },
                (sender, args, context) => { System.Console.WriteLine(args.Path); });

            var response = await _client.OnAsync("chat",
                (sender, args, context) => { System.Console.WriteLine(args.Data + " -> 2\n"); },
                (sender, args, context) => { System.Console.WriteLine(args.Data); },
                (sender, args, context) => { System.Console.WriteLine(args.Path); });

            //Call dispose to stop listening for events
            //response.Dispose();
        }

        public class Person
        {
            private List<double> _latitudeLongitude;

            #region Public Properties

            [JsonProperty(PropertyName = "l")]
            public List<double> LatitudeLongitude
            {
                get
                {
                    return _latitudeLongitude ?? (_latitudeLongitude = new List<double>());
                }
                set
                {
                    _latitudeLongitude = value;
                }
            }

            [JsonProperty(PropertyName = "fullName")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "dest")]
            public string Destination { get; set; }
            
            [JsonProperty(PropertyName = "key")]
            public string FirebaseKey { get; set; }

            #endregion

            #region Public Methods and Operators

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            }

            #endregion
        }
    }
}