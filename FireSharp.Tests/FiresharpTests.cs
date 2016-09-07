using Common.Testing.NUnit;
using FireSharp.Config;
using FireSharp.Exceptions;
using FireSharp.Tests.Models;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FireSharp.Tests
{
    public class FiresharpTests : FiresharpTestsBase
    {
        protected override async void FinalizeTearDown()
        {
            var task1 = FirebaseClient.DeleteAsync("todos");
            var task2 = FirebaseClient.DeleteAsync("fakepath");

            await Task.WhenAll(task1, task2);
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
        public async void GetAsync()
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
        public async void GetListAsync()
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
        public async void OnChangeGetAsync()
        {
            var id = Guid.NewGuid().ToString("N");

            var changes = new ConcurrentBag<Todo>();

            var expected = new Todo { name = "Execute PUSH4GET1", priority = 2 };
            
            var observer = FirebaseClient.OnChangeGetAsync<Todo>($"fakepath/{id}/OnGetAsync/", (events, arg) =>
            {
                changes.Add(arg);
            });

            await FirebaseClient.SetAsync($"fakepath/{id}/OnGetAsync/", expected);

            await Task.Delay(2000);

            await FirebaseClient.SetAsync($"fakepath/{id}/OnGetAsync/name", "PUSH4GET1");

            await Task.Delay(2000);

            try
            {
                Assert.AreEqual(2, changes.Count);

                Assert.AreEqual(0, changes.Count(todo => todo == null));
                Assert.AreEqual(1, changes.Count(todo => todo.name == expected.name));
                Assert.AreEqual(1, changes.Count(todo => todo.name == "PUSH4GET1"));
            }
            finally
            {
                observer.Result.Cancel();
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
        public async void PushAsync()
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

            await FirebaseClient.PushAsync("todos/get/pushAsync", new Todo
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
        public async void SetAsync()
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
        public async void UpdateAsync()
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
        public async void UpdateFailureAsync()
        {
            await AssertExtensions.ThrowsAsync<FirebaseException>(async () =>
            {
                var response = await FirebaseClient.UpdateAsync("todos", true);
            });
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async void GetWithQueryAsync()
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
    }
}