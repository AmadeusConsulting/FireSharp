using Common.Testing.NUnit;
using FireSharp.Config;
using FireSharp.Exceptions;
using FireSharp.Tests.Models;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
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

            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }
            
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
            var id = Guid.NewGuid().ToString("N");

            var changes = new ConcurrentBag<Todo>();

            var expected = new Todo { name = "Execute CHANGEGETASYNC", priority = 2 };

            var reset = new ManualResetEvent(false);

            var observer = await FirebaseClient.OnChangeGetAsync<Todo>(
                $"fakepath/{id}/OnGetAsync/",
                (events, arg) =>
                    {
                        changes.Add(arg);
                        if (changes.Count == 2)
                        {
                            reset.Set();
                        }
                    });

            await FirebaseClient.SetAsync($"fakepath/{id}/OnGetAsync/", expected);

            await FirebaseClient.SetAsync($"fakepath/{id}/OnGetAsync/name", "CHANGEGETASYNC-MODIFIED");

            reset.WaitOne(TimeSpan.FromSeconds(30));

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
            var response = FirebaseClient.Update("todos", true);
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async Task UpdateFailureAsync()
        {
            await AssertExtensions.ThrowsAsync<FirebaseException>(async () =>
            {
                var response = await FirebaseClient.UpdateAsync("todos", true);
            });
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async Task GetWithQueryAsync()
        {
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

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async Task GetWithNonStringStartEndQueryAsync()
        {
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

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async Task ListenToEntityList()
        {
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
            var changed = new Dictionary<string, Tuple<Todo, Todo, IEnumerable<string>>>(); // new, old, path

            var addedReset = new ManualResetEvent(false);
            var removedReset = new ManualResetEvent(false);
            var changedReset = new ManualResetEvent(false);

            var observer = await FirebaseClient.MonitorEntityListAsync<Todo>(
                TodosListLocation,
                (s, key, val) =>
                    {
                        added.Add(key, val);
                        if (added.Count == 6)
                        {
                            addedReset.Set();
                        }
                    },
                (s, key, paths, val, oldVal) =>
                    {
                        changed.Add(key, new Tuple<Todo, Todo, IEnumerable<string>>(val, oldVal, paths));
                        changedReset.Set();
                    },
                (s, key, val) =>
                    {
                        removed.Add(key, val);
                        removedReset.Set();
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

                addedReset.WaitOne(TimeSpan.FromSeconds(30));
                removedReset.WaitOne(TimeSpan.FromSeconds(30));
                changedReset.WaitOne(TimeSpan.FromSeconds(30));
                

                Assert.AreEqual(6, added.Count);
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo1Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo2Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo3Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo4Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo5Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo6Response.Result.Name));

                Assert.AreEqual(1, changed.Count);
                Assert.IsTrue(changed.Single().Key == todo4Response.Result.Name);
                Assert.Contains("priority", changed.Single().Value.Item3.ToList());
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

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async Task ListenToEntityListEntitiesAddedAfterListenStart()
        {
            const string TodosListLocation = "todos/entityList";
            
            var added = new Dictionary<string, Todo>();
            var removed = new Dictionary<string, Todo>();
            var changed = new Dictionary<string, Tuple<Todo, Todo, IEnumerable<string>>>(); // new, old, path

            var addedReset = new ManualResetEvent(false);
            var removedReset = new ManualResetEvent(false);
            var changedReset = new ManualResetEvent(false);

            var observer = await FirebaseClient.MonitorEntityListAsync<Todo>(
                TodosListLocation,
                (s, key, val) =>
                {
                    added.Add(key, val);
                    if (added.Count == 6)
                    {
                        addedReset.Set();
                    }
                },
                (s, key, paths, val, oldVal) =>
                {
                    changed.Add(key, new Tuple<Todo, Todo, IEnumerable<string>>(val, oldVal, paths));
                    changedReset.Set();
                },
                (s, key, val) =>
                {
                    removed.Add(key, val);
                    removedReset.Set();
                });

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

                addedReset.WaitOne(TimeSpan.FromSeconds(30));
                removedReset.WaitOne(TimeSpan.FromSeconds(30));
                changedReset.WaitOne(TimeSpan.FromSeconds(30));

                Assert.AreEqual(6, added.Count);
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo1Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo2Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo3Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo4Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo5Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo6Response.Result.Name));

                Assert.AreEqual(1, changed.Count);
                Assert.IsTrue(changed.Single().Key == todo4Response.Result.Name);
                Assert.Contains("priority", changed.Single().Value.Item3.ToList());
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

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async Task WriteToExistingRule()
        {
            var rulesClient = new FirebaseClient(new FirebaseConfig { BasePath = FirebaseUrl, AuthSecret = FirebaseSecret });

            var rules = await rulesClient.GetDatabaseRulesAsync();

            rules[UniquePathId]["existing-rules-test"] = new Dictionary<string, object>
                                               {
                                                   { ".read", "auth != null" }
                                               };

            await rulesClient.SetDatabaseRulesAsync(rules.Rules);

            rules = await rulesClient.GetDatabaseRulesAsync();

            var existingRules = rules[UniquePathId];

            Assert.IsInstanceOf<DatabaseRules>(existingRules["existing-rules-test"]);

            Assert.DoesNotThrow(() => existingRules["existing-rules-test"].Rules[".indexOn"] = "priority");
            
            await rulesClient.SetDatabaseRulesAsync(rules.Rules);
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async Task UpdateEntityListEntityWithNestedObject()
        {
            const string TodosListLocation = "todos/entityList";

            var added = new Dictionary<string, Todo>();
            var removed = new Dictionary<string, Todo>();
            var changed = new Dictionary<string, Tuple<Todo, Todo, IEnumerable<string>>>(); // new, old, path
            
            var changedReset = new ManualResetEvent(false);


            var observer = await FirebaseClient.MonitorEntityListAsync<Todo>(
                TodosListLocation,
                (s, key, val) =>
                {
                    added.Add(key, val);
                },
                (s, key, paths, val, oldVal) =>
                {
                    changed.Add(key, new Tuple<Todo, Todo, IEnumerable<string>>(val, oldVal, paths));
                    changedReset.Set();
                },
                (s, key, val) =>
                {
                    removed.Add(key, val);
                });

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
                  name = "Priority 1",
                  priority = 1
              });

            try
            {
                await FirebaseClient.SetAsync(
                    $"{TodosListLocation}/{todo2Response.Result.Name}/assignee",
                    new Assignee
                        {
                            firstName = "John",
                            lastName = "Doe",
                            position = "Manager"
                        });

                changedReset.WaitOne(TimeSpan.FromSeconds(30));

                Assert.AreEqual(2, added.Count);
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo1Response.Result.Name));
              
                Assert.AreEqual(1, changed.Count);
                Assert.IsTrue(changed.Single().Key == todo2Response.Result.Name);
                Assert.Contains("assignee", changed.Single().Value.Item3.ToList()); // changed path
                Assert.IsNotNull(changed.Single().Value.Item1.assignee); // new value
                Assert.IsNotNull(changed.Single().Value.Item1.assignee.firstName); 
                Assert.IsNotNull(changed.Single().Value.Item1.assignee.lastName); 
                Assert.IsNotNull(changed.Single().Value.Item1.assignee.position);
                Assert.IsNull(changed.Single().Value.Item2.assignee); // old value
            }
            finally
            {
                observer.Cancel();
            }
        }

        [Test]
        [Category("INTEGRATION")]
        [Category("ASYNC")]
        public async Task ConcurrentEntityStreaming()
        {
            const string TodosListLocation = "todos/entityList";
            const string TodosListLocation2 = "todos/entityList2";

            var added = new Dictionary<string, Todo>();
            var removed = new Dictionary<string, Todo>();
            var changed = new Dictionary<string, Tuple<Todo, Todo, IEnumerable<string>>>(); // new, old, path

            var added2 = new Dictionary<string, Todo>();
            var removed2 = new Dictionary<string, Todo>();
            var changed2 = new Dictionary<string, Tuple<Todo, Todo, IEnumerable<string>>>(); // new, old, path

            var addedReset = new ManualResetEvent(false);
            var removedReset = new ManualResetEvent(false);
            var changedReset = new ManualResetEvent(false);

            var addedReset2 = new ManualResetEvent(false);
            var removedReset2 = new ManualResetEvent(false);
            var changedReset2 = new ManualResetEvent(false);

            var log = Config.LogManager.GetLogger("ConcurrentEntityStreaming");

            var observer = await FirebaseClient.MonitorEntityListAsync<Todo>(
                TodosListLocation,
                (s, key, val) =>
                {
                    log.Info($"Added TODO {key} \n{val}");
                    added.Add(key, val);
                    if (added.Count == 3)
                    {
                        addedReset.Set();
                    }
                },
                (s, key, paths, val, oldVal) =>
                {
                    log.Info($"Changed TODO {paths} \n{oldVal}\n{val}");
                    changed.Add(key, new Tuple<Todo, Todo, IEnumerable<string>>(val, oldVal, paths));
                    changedReset.Set();
                },
                (s, key, val) =>
                {
                    log.Info($"Removed TODO {key} \n{val}");
                    removed.Add(key, val);
                    removedReset.Set();
                });

            var observer2 = await FirebaseClient.MonitorEntityListAsync<Todo>(
                TodosListLocation2,
                (s, key, val) =>
                {
                    log.Info($"Added TODO {key} \n{val}");
                    added2.Add(key, val);
                    if (added2.Count == 3)
                    {
                        addedReset2.Set();
                    }
                },
                (s, key, paths, val, oldVal) =>
                {
                    log.Info($"Changed TODO {paths} \n{oldVal}\n{val}");
                    changed2.Add(key, new Tuple<Todo, Todo, IEnumerable<string>>(val, oldVal, paths));
                    changedReset2.Set();
                },
                (s, key, val) =>
                {
                    log.Info($"Removed TODO {key} \n{val}");
                    removed2.Add(key, val);
                    removedReset2.Set();
                });

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
            
            var todo1Response2 = await FirebaseClient.PushAsync(
             TodosListLocation2,
             new Todo
             {
                 name = "Priority 1",
                 priority = 1
             });

            var todo2Response2 = await FirebaseClient.PushAsync(
                TodosListLocation2,
                new Todo
                {
                    name = "Priority 2",
                    priority = 2
                });

            try
            {
                // 1
                await FirebaseClient.SetAsync($"{TodosListLocation}/{todo2Response.Result.Name}/priority", 99);

                await FirebaseClient.DeleteAsync($"{TodosListLocation}/{todo1Response.Result.Name}");

                var todo3Response = await FirebaseClient.PushAsync(
                    TodosListLocation,
                    new Todo
                    {
                        name = "Priority 3",
                        priority = 6
                    });

                // 2
                await FirebaseClient.SetAsync($"{TodosListLocation2}/{todo2Response2.Result.Name}/priority", 99);

                await FirebaseClient.DeleteAsync($"{TodosListLocation2}/{todo1Response2.Result.Name}");

                var todo3Response2 = await FirebaseClient.PushAsync(
                    TodosListLocation2,
                    new Todo
                    {
                        name = "Priority 3",
                        priority = 6
                    });

                addedReset.WaitOne(TimeSpan.FromSeconds(30));
                changedReset.WaitOne(TimeSpan.FromSeconds(30));
                removedReset.WaitOne(TimeSpan.FromSeconds(30));

                addedReset2.WaitOne(TimeSpan.FromSeconds(30));
                changedReset2.WaitOne(TimeSpan.FromSeconds(30));
                removedReset2.WaitOne(TimeSpan.FromSeconds(30));

                // 1
                Assert.AreEqual(3, added.Count);
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo1Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo2Response.Result.Name));
                Assert.IsTrue(added.Any(kvp => kvp.Key == todo3Response.Result.Name));
                

                Assert.AreEqual(1, changed.Count);
                Assert.IsTrue(changed.Single().Key == todo2Response.Result.Name);
                Assert.Contains("priority", changed.Single().Value.Item3.ToList());
                Assert.AreEqual(changed.Single().Value.Item1.priority, 99, $"Expected new priority = 99.\nNew Value:\n{changed.Single().Value.Item1}\nOld Value:\n{changed.Single().Value.Item2}"); // new value
                Assert.AreEqual(changed.Single().Value.Item2.priority, 2); // old value

                Assert.AreEqual(1, removed.Count);
                Assert.IsTrue(removed.Single().Key == todo1Response.Result.Name);

                // 2
                Assert.AreEqual(3, added2.Count);
                Assert.IsTrue(added2.Any(kvp => kvp.Key == todo1Response2.Result.Name));
                Assert.IsTrue(added2.Any(kvp => kvp.Key == todo2Response2.Result.Name));
                Assert.IsTrue(added2.Any(kvp => kvp.Key == todo3Response2.Result.Name));


                Assert.AreEqual(1, changed2.Count);
                Assert.IsTrue(changed2.Single().Key == todo2Response2.Result.Name);
                Assert.Contains("priority", changed2.Single().Value.Item3.ToList());
                Assert.AreEqual(changed2.Single().Value.Item1.priority, 99); // new value
                Assert.AreEqual(changed2.Single().Value.Item2.priority, 2); // old value

                Assert.AreEqual(1, removed2.Count);
                Assert.IsTrue(removed2.Single().Key == todo1Response2.Result.Name);
            }
            finally
            {
                observer.Cancel();
                observer2.Cancel();
            }
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async Task FullValuePathReceivedInChangedEventOnSetValue()
        {
            const string TodosListLocation = "todos/entityListPatch";


            var todo1Response = await FirebaseClient.PushAsync(
                TodosListLocation,
                new Todo
                {
                    name = "Priority 1",
                    priority = 1
                });
            
            var reset = new ManualResetEvent(false);

            var added = new Dictionary<string, Todo>();
            var removed = new Dictionary<string, Todo>();
            var changed = new List<KeyValuePair<string, Tuple<Todo, Todo, IEnumerable<string>>>>(); // new, old, path

            var observer = await FirebaseClient.MonitorEntityListAsync<Todo>(
                               TodosListLocation,
                               (s, key, val) =>
                                   {
                                       added.Add(key, val);
                                   },
                               (s, key, paths, val, oldVal) =>
                                   {
                                       changed.Add(
                                           new KeyValuePair<string, Tuple<Todo, Todo, IEnumerable<string>>>(
                                               key,
                                               new Tuple<Todo, Todo, IEnumerable<string>>(val, oldVal, paths)));
                                       if (changed.Count == 2)
                                       {
                                           reset.Set();
                                       }
                                   },
                               (s, key, val) =>
                                   {
                                       removed.Add(key, val);
                                   });

            try
            {
                await FirebaseClient.SetAsync($"{TodosListLocation}/{todo1Response.Result.Name}/priority", 99);

                await FirebaseClient.UpdateAsync(
                    $"{TodosListLocation}/{todo1Response.Result.Name}",
                    new Dictionary<string, object>
                        {
                                { "priority", 100 }
                        });

                reset.WaitOne(TimeSpan.FromSeconds(30));

                Assert.AreEqual(2, changed.Count);
                Assert.IsTrue(changed.All(c => c.Value.Item3.Contains("priority")));
                Assert.IsTrue(changed.All(c => c.Key == todo1Response.Result.Name));
                Assert.IsTrue(changed.First().Value.Item1.priority == 99);
                Assert.IsTrue(changed.Skip(1).Single().Value.Item1.priority == 100);
            }
            finally
            {
                observer.Cancel();
            }
        }
    }
}