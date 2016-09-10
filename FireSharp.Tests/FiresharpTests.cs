using Common.Testing.NUnit;
using FireSharp.Config;
using FireSharp.Exceptions;
using FireSharp.Tests.Models;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FireSharp.Tests
{
    public class FiresharpTests : FiresharpTestsBase
    {
        private string _rulesUrl;

        [SetUp]
        public async void SetUp()
        {
            var task1 = FirebaseClient.DeleteAsync("todos");
            var task2 = FirebaseClient.DeleteAsync("fakepath");


            await Task.WhenAll(task1, task2);
        }

        protected override string SetUpUniqueFirebaseUrlPath()
        {
            var httpClient = new HttpClient();

            _rulesUrl = $"{FirebaseUrl}.settings/rules.json?auth={FirebaseSecret}";

            string uniqueId = base.SetUpUniqueFirebaseUrlPath();

            var rules = GetFirebaseRules(_rulesUrl, httpClient);

            rules[uniqueId] = new JObject();
            var uniqueIdRules = (JObject)rules[uniqueId];
            uniqueIdRules["todos"] = new JObject(
                new JObject
                    {
                        {
                            "get", new JObject(
                            new JObject
                                {
                                    {
                                        "pushAsync", new JObject
                                                         {
                                                             { ".indexOn", "priority" }
                                                         }
                                    }
                                })
                        }
                    });
           
            
            SetFirebaseRules(rules, _rulesUrl, httpClient);

            return uniqueId;
        }

        private static void SetFirebaseRules(JObject rules, string rulesUrl, HttpClient httpClient)
        {
            var rulesWrapper = new JObject
                                   {
                                       ["rules"] = rules
                                   };

            var rulesUpdateRequest = new HttpRequestMessage(HttpMethod.Put, rulesUrl)
                                         {
                                             Content = new StringContent(JsonConvert.SerializeObject(rulesWrapper), Encoding.UTF8, "application/json")
                                         };

            var updateRulesResponse = httpClient.SendAsync(rulesUpdateRequest).Result;

            if (updateRulesResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(
                    $"Response status code for updating rules did not indicate success: {updateRulesResponse.StatusCode}");
            }
        }

        private JObject GetFirebaseRules(string rulesUrl, HttpClient httpClient)
        {
            var rulesGetRequest = new HttpRequestMessage(HttpMethod.Get, rulesUrl);

            var rulesResponse = httpClient.SendAsync(rulesGetRequest).Result;

            var rulesResponseContent = JsonConvert.DeserializeObject<Dictionary<string, object>>(rulesResponse.Content.ReadAsStringAsync().Result);

            var rules = (JObject)rulesResponseContent["rules"];
            
            return rules;
        }

        [TearDown]
        public void TearDown()
        {
            var httpClient = new HttpClient();

            var rules = GetFirebaseRules(_rulesUrl, httpClient);

            rules.Remove(UniquePathId);

            var task1 = FirebaseClient.DeleteAsync("todos");
            var task2 = FirebaseClient.DeleteAsync("fakepath");

            Task.WhenAll(task1, task2).Wait();
        }

        [Test, Category("INTEGRATION")]
        public void Delete()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            FirebaseClient.Push("todos/push", new Todo
            {
                name = "Execute PUSH4GET",
                priority = 2
            });

            var response = FirebaseClient.Delete("todos/push");
            Assert.NotNull(response);
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public void DeleteAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            FirebaseClient.PushAsync("todos/pushAsync", new Todo
            {
                name = "Execute PUSH4GET",
                priority = 2
            }).Wait();

            var response = FirebaseClient.DeleteAsync("todos/pushAsync").Result;
            Assert.NotNull(response);
        }

        [Test, Category("INTEGRATION"), Category("SYNC")]
        public void Get()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            FirebaseClient.Push("todos/gettest/push", new Todo
            {
                name = "Execute PUSH4GET",
                priority = 2
            });

            Thread.Sleep(400);

            var response = FirebaseClient.Get("todos/gettest");
            Assert.NotNull(response);
            Assert.IsTrue(response.Body.Contains("name"));
            Assert.IsTrue(response.Body.Contains("Execute PUSH4GET"));
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public void GetAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            FirebaseClient.PushAsync("todos/get/pushAsync", new Todo
            {
                name = "Execute PUSH4GET",
                priority = 2
            }).Wait();

            Thread.Sleep(400);

            var response = FirebaseClient.GetAsync("todos/get/").Result;
            Assert.NotNull(response);
            Assert.IsTrue(response.Body.Contains("name"));
        }

        [Test, Category("INTEGRATION")]
        public void GetListAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            var expected = new List<Todo>
            {
                new Todo {name = "Execute PUSH4GET1", priority = 2},
                new Todo {name = "Execute PUSH4GET2", priority = 2},
                new Todo {name = "Execute PUSH4GET3", priority = 2},
                new Todo {name = "Execute PUSH4GET4", priority = 2},
                new Todo {name = "Execute PUSH4GET5", priority = 2}
            };

            var pushResponse = FirebaseClient.PushAsync("todos/list/pushAsync", expected).Result;
            var id = pushResponse.Result.Name;


#pragma warning disable 618 // Point of the test
            Assert.AreEqual(pushResponse.Result.name, pushResponse.Result.Name);
#pragma warning restore 618

            Thread.Sleep(400);

            var getResponse = FirebaseClient.GetAsync(string.Format("todos/list/pushAsync/{0}", id)).Result;

            var actual = getResponse.ResultAs<List<Todo>>();

            Assert.NotNull(pushResponse);
            Assert.NotNull(getResponse);
            Assert.NotNull(actual);
            Assert.AreEqual(expected.Count, actual.Count);
        }

        [Test, Category("INTEGRATION")]
        public void OnChangeGetAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            var id = Guid.NewGuid().ToString("N");

            var changes = new ConcurrentBag<Todo>();

            var expected = new Todo { name = "Execute CHANGEGETASYNC", priority = 2 };
            
            var observer = FirebaseClient.OnChangeGetAsync<Todo>($"fakepath/{id}/OnGetAsync/", (events, arg) =>
            {
                changes.Add(arg);
            });

            FirebaseClient.SetAsync($"fakepath/{id}/OnGetAsync/", expected).Wait();

            Task.Delay(2000).Wait();

            FirebaseClient.SetAsync($"fakepath/{id}/OnGetAsync/name", "CHANGEGETASYNC-MODIFIED").Wait();

            Task.Delay(2000).Wait();

            try
            {
                Assert.AreEqual(2, changes.Count);

                Assert.AreEqual(0, changes.Count(todo => todo == null));
                Assert.AreEqual(1, changes.Count(todo => todo.name == expected.name));
                Assert.AreEqual(1, changes.Count(todo => todo.name == "CHANGEGETASYNC-MODIFIED"));
            }
            finally
            {
                observer.Result.Cancel();
            }
        }

        [Test, Category("INTEGRATION"), Category("SYNC")]
        public void Push()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            var todo = new Todo
            {
                name = "Execute PUSH4",
                priority = 2
            };

            var response = FirebaseClient.Push("todos/push", todo);
            Assert.NotNull(response);
            Assert.NotNull(response.Result);
            Assert.NotNull(response.Result.Name); /*Returns pushed data name like -J8LR7PDCdz_i9H41kf7*/
            Console.WriteLine(response.Result.Name);
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public void PushAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            var todo = new Todo
            {
                name = "Execute PUSH4",
                priority = 2
            };

            var response = FirebaseClient.PushAsync("todos/push/pushAsync", todo).Result;
            Assert.NotNull(response);
            Assert.NotNull(response.Result);
            Assert.NotNull(response.Result.Name); /*Returns pushed data name like -J8LR7PDCdz_i9H41kf7*/
            Console.WriteLine(response.Result.Name);
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public void SecondConnectionWithoutSlash()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            // This integration test will write from _config but read from a second Firebase connection to
            // the same DB, but with a BasePath which does not contain the unnecessary trailing slash.
            var secondClientToTest = new FirebaseClient(new FirebaseConfig
            {
                AuthSecret = FirebaseSecret,
                BasePath = FirebaseUrlWithoutSlash
            });

            FirebaseClient.PushAsync("todos/get/pushAsync", new Todo
            {
                name = "SecondConnectionWithoutSlash",
                priority = 3
            }).Wait();

            Thread.Sleep(400);

            var response = secondClientToTest.GetAsync("todos/get/").Result;
            Assert.NotNull(response);
            Assert.IsTrue(response.Body.Contains("name"));
            Assert.IsTrue(response.Body.Contains("SecondConnectionWithoutSlash"));
        }

        [Test, Category("INTEGRATION"), Category("SYNC")]
        public void Set()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            var todo = new Todo
            {
                name = "Execute SET",
                priority = 2
            };
            var response = FirebaseClient.Set("todos/set", todo);
            var result = response.ResultAs<Todo>();
            Assert.NotNull(response);
            Assert.AreEqual(todo.name, result.name);

            // overwrite the todo we just set
            response = FirebaseClient.Set("todos", todo);
            var getResponse = FirebaseClient.Get("/todos/set");
            result = getResponse.ResultAs<Todo>();
            Assert.Null(result);
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public void SetAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            var todo = new Todo
            {
                name = "Execute SET",
                priority = 2
            };
            var response = FirebaseClient.SetAsync("todos/setAsync", todo).Result;
            var result = response.ResultAs<Todo>();
            Assert.NotNull(response);
            Assert.AreEqual(todo.name, result.name);

            // overwrite the todo we just set
            response = FirebaseClient.SetAsync("todos", todo).Result;
            var getResponse = FirebaseClient.GetAsync("/todos/setAsync").Result;
            result = getResponse.ResultAs<Todo>();
            Assert.Null(result);
        }

        [Test, Category("INTEGRATION"), Category("SYNC")]
        public void Update()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            FirebaseClient.Set("todos/updatetest/set", new Todo
            {
                name = "Execute SET",
                priority = 2
            });

            var todoToUpdate = new Todo
            {
                name = "Execute UPDATE!",
                priority = 1
            };

            var response = FirebaseClient.Update("todos/updatetest/set", todoToUpdate);
            Assert.NotNull(response);
            var actual = response.ResultAs<Todo>();
            Assert.AreEqual(todoToUpdate.name, actual.name);
            Assert.AreEqual(todoToUpdate.priority, actual.priority);
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public void UpdateAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            FirebaseClient.SetAsync("todos/set/setAsync", new Todo
            {
                name = "Execute SET",
                priority = 2
            }).Wait();

            var todoToUpdate = new Todo
            {
                name = "Execute UPDATE!",
                priority = 1
            };

            var response = FirebaseClient.UpdateAsync("todos/set/setAsync", todoToUpdate).Result;
            Assert.NotNull(response);
            var actual = response.ResultAs<Todo>();
            Assert.AreEqual(todoToUpdate.name, actual.name);
            Assert.AreEqual(todoToUpdate.priority, actual.priority);
        }

        [Test, ExpectedException(typeof(FirebaseException)), Category("INTEGRATION"), Category("SYNC")]
        public void UpdateFailure()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            var response = FirebaseClient.Update("todos", true);
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public void UpdateFailureAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            AssertExtensions.ThrowsAsync<FirebaseException>(async () =>
            {
                var response = await FirebaseClient.UpdateAsync("todos", true);
            }).Wait();
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public void GetWithQueryAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            FirebaseClient.PushAsync("todos/get/pushAsync", new Todo
            {
                name = "Execute PUSH4GET",
                priority = 2
            }).Wait();

            FirebaseClient.PushAsync("todos/get/pushAsync", new Todo
            {
                name = "You PUSH4GET",
                priority = 2
            }).Wait();

            Thread.Sleep(400);

            var response = FirebaseClient.GetAsync("todos", QueryBuilder.New().OrderBy("$key").StartAt("Exe")).Result;
            Assert.NotNull(response);
            Assert.IsTrue(response.Body.Contains("name"));
        }

        [Test]
        public void GetWithNonStringStartEndQueryAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            const string TodosPushLocation = "todos/get/pushAsync";

            FirebaseClient.PushAsync(
                TodosPushLocation,
                new Todo
                    {
                        name = "Priority 1",
                        priority = 1
                    }).Wait();

            FirebaseClient.PushAsync(
                TodosPushLocation,
                new Todo
                    {
                        name = "Priority 2",
                        priority = 2
                    }).Wait();

            FirebaseClient.PushAsync(
                TodosPushLocation,
                new Todo
                    {
                        name = "Priority 3",
                        priority = 3
                    }).Wait();

            FirebaseClient.PushAsync(
                TodosPushLocation,
                new Todo
                {
                    name = "Priority 4",
                    priority = 4
                }).Wait();

            FirebaseClient.PushAsync(
                TodosPushLocation,
                new Todo
                    {
                        name = "Priority 5",
                        priority = 5
                    }).Wait();

            var response = FirebaseClient.GetAsync(TodosPushLocation, QueryBuilder.New().OrderBy("priority").StartAt(2).EndAt(4)).Result;
            Assert.NotNull(response);
            Assert.IsTrue(response.Body.Contains("Priority 4") && response.Body.Contains("Priority 3") && response.Body.Contains("Priority 2"));
            Assert.IsFalse(response.Body.Contains("Priority 1") || response.Body.Contains("Priority 5"));
        }
    }
}