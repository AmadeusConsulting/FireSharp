using Common.Testing.NUnit;
using FireSharp.Config;
using FireSharp.Exceptions;
using FireSharp.Tests.Models;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FireSharp.Tests
{
    public class FiresharpTests : FiresharpTestsBase
    {
        [SetUp]
        protected override void FinalizeSetUp()
        {
            base.FinalizeSetUp();

            var task1 = FirebaseClient.DeleteAsync("todos");
            var task2 = FirebaseClient.DeleteAsync("fakepath");
            
            Task.WhenAll(task1, task2).Wait();
        }

        protected override void SetupFirebaseRules()
        {
            base.SetupFirebaseRules();

            if (FirebaseClient != null)
            {
                var rulesClient = new FirebaseClient(new FirebaseConfig {BasePath = FirebaseUrl, AuthSecret = FirebaseSecret });

                var rules = rulesClient.GetDatabaseRulesAsync().Result;

                rules[UniquePathId]["todos"]["get"]["pushAsync"] = DatabaseRules.Create(
                    new Dictionary<string, object>
                        {
                            { ".indexOn", "priority" }
                        });

                rulesClient.SetDatabaseRulesAsync(rules.Rules).Wait();

                Task.Delay(2000).Wait();
            }
        }

        protected override void TearDownFirebaseRules()
        {
            base.TearDownFirebaseRules();

            if (FirebaseClient != null)
            {
                var rulesClient = new FirebaseClient(new FirebaseConfig { BasePath = FirebaseUrl, AuthSecret = FirebaseSecret });
                var rules = rulesClient.GetDatabaseRulesAsync().Result;

                rules.Remove(UniquePathId);

                var task1 = FirebaseClient.DeleteAsync("todos");
                var task2 = FirebaseClient.DeleteAsync("fakepath");
                var task3 = rulesClient.SetDatabaseRulesAsync(rules.Rules);

                Task.WhenAll(task1, task2, task3).Wait();
            }
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
        public async Task DeleteAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            await FirebaseClient.PushAsync("todos/pushAsync", new Todo
            {
                name = "Execute PUSH4GET",
                priority = 2
            });

            var response = await FirebaseClient.DeleteAsync("todos/pushAsync");
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
        public async Task GetAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            await FirebaseClient.PushAsync("todos/get/pushAsync", new Todo
            {
                name = "Execute PUSH4GET",
                priority = 2
            });

            Thread.Sleep(400);

            var response = await FirebaseClient.GetAsync("todos/get/");
            Assert.NotNull(response);
            Assert.IsTrue(response.Body.Contains("name"));
        }

        [Test, Category("INTEGRATION")]
        public async Task GetListAsync()
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

            var pushResponse = await FirebaseClient.PushAsync("todos/list/pushAsync", expected);
            var id = pushResponse.Result.Name;


#pragma warning disable 618 // Point of the test
            Assert.AreEqual(pushResponse.Result.name, pushResponse.Result.Name);
#pragma warning restore 618

            Thread.Sleep(400);

            var getResponse = await FirebaseClient.GetAsync(string.Format("todos/list/pushAsync/{0}", id));

            var actual = getResponse.ResultAs<List<Todo>>();

            Assert.NotNull(pushResponse);
            Assert.NotNull(getResponse);
            Assert.NotNull(actual);
            Assert.AreEqual(expected.Count, actual.Count);
        }

        [Test, Category("INTEGRATION")]
        public async Task OnChangeGetAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            var id = Guid.NewGuid().ToString("N");

            var changes = new ConcurrentBag<Todo>();

            var expected = new Todo { name = "Execute CHANGEGETASYNC", priority = 2 };

            var observer = await FirebaseClient.OnChangeGetAsync<Todo>(
                $"fakepath/{id}/OnGetAsync/",
                (events, arg) =>
                    {
                        changes.Add(arg);
                    });

            await FirebaseClient.SetAsync($"fakepath/{id}/OnGetAsync/", expected);

            await Task.Delay(2000);

            await FirebaseClient.SetAsync($"fakepath/{id}/OnGetAsync/name", "CHANGEGETASYNC-MODIFIED");

            await Task.Delay(2000);

            try
            {
                Assert.AreEqual(2, changes.Count);

                Assert.AreEqual(0, changes.Count(todo => todo == null));
                Assert.AreEqual(1, changes.Count(todo => todo.name == expected.name));
                Assert.AreEqual(1, changes.Count(todo => todo.name == "CHANGEGETASYNC-MODIFIED"));
            }
            finally
            {
                observer.Cancel();
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
        public async Task PushAsync()
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

            var response = await FirebaseClient.PushAsync("todos/push/pushAsync", todo);
            Assert.NotNull(response);
            Assert.NotNull(response.Result);
            Assert.NotNull(response.Result.Name); /*Returns pushed data name like -J8LR7PDCdz_i9H41kf7*/
            Console.WriteLine(response.Result.Name);
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async Task SecondConnectionWithoutSlash()
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

            await FirebaseClient.PushAsync(
                "todos/get/pushAsync",
                new Todo
                    {
                        name = "SecondConnectionWithoutSlash",
                        priority = 3
                    });

            Thread.Sleep(400);

            var response = await secondClientToTest.GetAsync("todos/get/");
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
        public async Task SetAsync()
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
            var response = await FirebaseClient.SetAsync("todos/setAsync", todo);
            var result = response.ResultAs<Todo>();
            Assert.NotNull(response);
            Assert.AreEqual(todo.name, result.name);

            // overwrite the todo we just set
            response = await FirebaseClient.SetAsync("todos", todo);
            var getResponse = await FirebaseClient.GetAsync("/todos/setAsync");
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
        public async Task UpdateAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            await FirebaseClient.SetAsync("todos/set/setAsync", new Todo
            {
                name = "Execute SET",
                priority = 2
            });

            var todoToUpdate = new Todo
            {
                name = "Execute UPDATE!",
                priority = 1
            };

            var response = await FirebaseClient.UpdateAsync("todos/set/setAsync", todoToUpdate);
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
        public async Task UpdateFailureAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            await AssertExtensions.ThrowsAsync<FirebaseException>(async () =>
            {
                var response = await FirebaseClient.UpdateAsync("todos", true);
            });
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async Task GetWithQueryAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            await FirebaseClient.PushAsync("todos/get/pushAsync", new Todo
            {
                name = "Execute PUSH4GET",
                priority = 2
            });

            await FirebaseClient.PushAsync("todos/get/pushAsync", new Todo
            {
                name = "You PUSH4GET",
                priority = 2
            });

            Thread.Sleep(400);

            var response = await FirebaseClient.GetAsync("todos", QueryBuilder.New().OrderBy("$key").StartAt("Exe"));
            Assert.NotNull(response);
            Assert.IsTrue(response.Body.Contains("name"));
        }

        [Test]
        public async Task GetWithNonStringStartEndQueryAsync()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            const string TodosPushLocation = "todos/get/pushAsync";

            await FirebaseClient.PushAsync(
                TodosPushLocation,
                new Todo
                    {
                        name = "Priority 1",
                        priority = 1
                    });

            await FirebaseClient.PushAsync(
                TodosPushLocation,
                new Todo
                    {
                        name = "Priority 2",
                        priority = 2
                    });

            await FirebaseClient.PushAsync(
                TodosPushLocation,
                new Todo
                    {
                        name = "Priority 3",
                        priority = 3
                    });

            await FirebaseClient.PushAsync(
                TodosPushLocation,
                new Todo
                    {
                        name = "Priority 4",
                        priority = 4
                    });

            await FirebaseClient.PushAsync(
                TodosPushLocation,
                new Todo
                    {
                        name = "Priority 5",
                        priority = 5
                    });

            var response = await FirebaseClient.GetAsync(TodosPushLocation, QueryBuilder.New().OrderBy("priority").StartAt(2).EndAt(4));
            Assert.NotNull(response);
            Assert.IsTrue(response.Body.Contains("Priority 4") && response.Body.Contains("Priority 3") && response.Body.Contains("Priority 2"));
            Assert.IsFalse(response.Body.Contains("Priority 1") || response.Body.Contains("Priority 5"));
        }

        [Test]
        public async Task ListenToEntityList()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            const string TodosListLocation = "todos/entityList";


            var todo1Response = await FirebaseClient.PushAsync(
                TodosListLocation,
                new Todo
                {
                    name = "Priority 1",
                    priority = 1
                });

            var todo2Response = await FirebaseClient.PushAsync(
                TodosListLocation,
                new Todo
                {
                    name = "Priority 2",
                    priority = 2
                });

            var todo3Response = await FirebaseClient.PushAsync(
                TodosListLocation,
                new Todo
                {
                    name = "Priority 3",
                    priority = 3
                });

            var todo4Response = await FirebaseClient.PushAsync(
                TodosListLocation,
                new Todo
                {
                    name = "Priority 4",
                    priority = 4
                });

            var todo5Response = await FirebaseClient.PushAsync(
                TodosListLocation,
                new Todo
                {
                    name = "Priority 5",
                    priority = 5
                });

            var added = new Dictionary<string, Todo>();
            var removed = new Dictionary<string ,Todo>();
            var changed = new Dictionary<string, Tuple<Todo, Todo, string>>(); // new, old, path

            var observer = await FirebaseClient.MonitorEntityListAsync<Todo>(
                TodosListLocation,
                (s, key, val) =>
                    {
                        added.Add(key, val);
                    },
                (s, key, path, val, oldVal) =>
                    {
                        changed.Add(key, new Tuple<Todo, Todo, string>(val, oldVal, path));
                    },
                (s, key, val) =>
                    {
                        removed.Add(key, val);
                    });
            
            try
            {
                await FirebaseClient.SetAsync($"{TodosListLocation}/{todo4Response.Result.Name}/priority", 99);

                await FirebaseClient.DeleteAsync($"{TodosListLocation}/{todo5Response.Result.Name}");

                var todo6Response = await FirebaseClient.PushAsync(
                    TodosListLocation,
                    new Todo
                        {
                            name = "Priority 6",
                            priority = 6
                        });

                await Task.Delay(1000);

                Assert.AreEqual(6, added.Count);
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo1Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo2Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo3Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo4Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo5Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo6Response.Result.Name));

                Assert.AreEqual(1, changed.Count);
                Assert.IsTrue(changed.Single().Key == todo4Response.Result.Name);
                Assert.AreEqual(changed.Single().Value.Item3, "priority");
                Assert.AreEqual(changed.Single().Value.Item1.priority, 99); // new value
                Assert.AreEqual(changed.Single().Value.Item2.priority, 4); // old value

                Assert.AreEqual(1, removed.Count);
                Assert.IsTrue(removed.Single().Key == todo5Response.Result.Name);
            }
            finally
            {
                observer.Cancel();
            }
        }
    }
}