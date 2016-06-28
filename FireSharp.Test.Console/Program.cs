using System;
using System.Collections.Generic;

using FireSharp.Config;
using FireSharp.EventStreaming;
using FireSharp.Interfaces;

using Newtonsoft.Json;

using Formatting = System.Xml.Formatting;

namespace FireSharp.Test.Console
{
    public class Program
    {
        protected const string BasePath = "https://project-7111630189540047655.firebaseio.com";
        protected const string FirebaseSecret = "o9BhBKBMTZ9fnLUYj9PnQ9GNeDfCv2RBJIwVUeeg";
        private static FirebaseClient _client;

        private static void Main()
        {
            IFirebaseConfig config = new FirebaseConfig
            {
                AuthSecret = FirebaseSecret,
                BasePath = BasePath
            };

            _client = new FirebaseClient(config); //Uses JsonNet default
            TCEventStreaming();
            PersonGenerator();
            //EventStreaming();
            //Crud();

            System.Console.Read();
        }

        private static async void PersonGenerator()
        {
            var destinations = new[] { "The Mall", "Home", "School", "Work", "Grocery Store" };

            for (int i = 0; i < 100; i++)
            {
                var person = new Person
                                 {
                                     Name = $"Person No. {i}",
                                     Destination = destinations[i % 5],
                                     LatitudeLongitude = new List<double>
                                                             {
                                                                 36.999084,
                                                                 -109.045224
                                                             }
                                 };
                var personRef = await _client.PushAsync("persons", person);
                person.FirebaseKey = personRef.Result.Name;
                await _client.UpdateAsync($"persons/{personRef.Result.Name}", person);
            }
        }

        private static async void Crud()
        {
            var setResponse = await _client.SetAsync("todos", new {name = "SET CALL"});
            System.Console.WriteLine(setResponse.Body);
        }

        private static async void TCEventStreaming()
        {
            await _client.MonitorEntityListAsync<Person>(
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
                new InMemoryEntityResponseCache<Person>()).ConfigureAwait(false);
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