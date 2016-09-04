using Common.Testing.NUnit;
using FireSharp.Config;
using FireSharp.Exceptions;
using FireSharp.Interfaces;
using FireSharp.Tests.Models;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FireSharp.Tests
{
    public class FiresharpTests : TestBase
    {
        private string _basePath;

        private string _basePathWithoutSlash;

        private string _firebaseSecret;

        private IFirebaseClient _client;

        [TestFixtureSetUp]
        public async void TestFixtureSetUp()
        {
            _basePath = ConfigurationManager.AppSettings["FireSharp.Tests.FirebaseUrl"];
            _firebaseSecret = ConfigurationManager.AppSettings["FireSharp.Tests.FirebaseSecret"];

            if (!_basePath.EndsWith("/"))
            {
                _basePath = $"{_basePath}/";
            }

            var uniqueId = Guid.NewGuid().ToString("N");

            _basePath = $"{_basePath}{uniqueId}/";
            _basePathWithoutSlash = _basePath.Substring(0, _basePath.Length - 1);

            IFirebaseConfig config = new FirebaseConfig
            {
                AuthSecret = _firebaseSecret,
                BasePath = _basePath
            };
            _client = new FirebaseClient(config); //Uses Newtonsoft.Json Json Serializer
        }

        protected override async void FinalizeTearDown()
        {
            var task1 = _client.DeleteAsync("todos");
            var task2 = _client.DeleteAsync("fakepath");

            await Task.WhenAll(task1, task2);
        }

        [Test, Category("INTEGRATION")]
        public void Delete()
        {
            _client.Push("todos/push", new Todo
            {
                name = "Execute PUSH4GET",
                priority = 2
            });

            var response = _client.Delete("todos/push");
            Assert.NotNull(response);
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async Task DeleteAsync()
        {
            await _client.PushAsync("todos/pushAsync", new Todo
            {
                name = "Execute PUSH4GET",
                priority = 2
            });

            var response = await _client.DeleteAsync("todos/pushAsync");
            Assert.NotNull(response);
        }

        [Test, Category("INTEGRATION"), Category("SYNC")]
        public void Get()
        {
            _client.Push("todos/gettest/push", new Todo
            {
                name = "Execute PUSH4GET",
                priority = 2
            });

            Thread.Sleep(400);

            var response = _client.Get("todos/gettest");
            Assert.NotNull(response);
            Assert.IsTrue(response.Body.Contains("name"));
            Assert.IsTrue(response.Body.Contains("Execute PUSH4GET"));
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async void GetAsync()
        {
            await _client.PushAsync("todos/get/pushAsync", new Todo
            {
                name = "Execute PUSH4GET",
                priority = 2
            });

            Thread.Sleep(400);

            var response = await _client.GetAsync("todos/get/");
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

            var pushResponse = await _client.PushAsync("todos/list/pushAsync", expected);
            var id = pushResponse.Result.Name;


#pragma warning disable 618 // Point of the test
            Assert.AreEqual(pushResponse.Result.name, pushResponse.Result.Name);
#pragma warning restore 618

            Thread.Sleep(400);

            var getResponse = await _client.GetAsync(string.Format("todos/list/pushAsync/{0}", id));

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
            
            var observer = _client.OnChangeGetAsync<Todo>($"fakepath/{id}/OnGetAsync/", (events, arg) =>
            {
                changes.Add(arg);
            });

            await _client.SetAsync($"fakepath/{id}/OnGetAsync/", expected);

            await Task.Delay(2000);

            await _client.SetAsync($"fakepath/{id}/OnGetAsync/name", "PUSH4GET1");

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

            var response = _client.Push("todos/push", todo);
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

            var response = await _client.PushAsync("todos/push/pushAsync", todo);
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
                AuthSecret = _firebaseSecret,
                BasePath = _basePathWithoutSlash
            });

            await _client.PushAsync("todos/get/pushAsync", new Todo
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
            var response = _client.Set("todos/set", todo);
            var result = response.ResultAs<Todo>();
            Assert.NotNull(response);
            Assert.AreEqual(todo.name, result.name);

            // overwrite the todo we just set
            response = _client.Set("todos", todo);
            var getResponse = _client.Get("/todos/set");
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
            var response = await _client.SetAsync("todos/setAsync", todo);
            var result = response.ResultAs<Todo>();
            Assert.NotNull(response);
            Assert.AreEqual(todo.name, result.name);

            // overwrite the todo we just set
            response = await _client.SetAsync("todos", todo);
            var getResponse = await _client.GetAsync("/todos/setAsync");
            result = getResponse.ResultAs<Todo>();
            Assert.Null(result);
        }

        [Test, Category("INTEGRATION"), Category("SYNC")]
        public void Update()
        {
            _client.Set("todos/updatetest/set", new Todo
            {
                name = "Execute SET",
                priority = 2
            });

            var todoToUpdate = new Todo
            {
                name = "Execute UPDATE!",
                priority = 1
            };

            var response = _client.Update("todos/updatetest/set", todoToUpdate);
            Assert.NotNull(response);
            var actual = response.ResultAs<Todo>();
            Assert.AreEqual(todoToUpdate.name, actual.name);
            Assert.AreEqual(todoToUpdate.priority, actual.priority);
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async void UpdateAsync()
        {
            await _client.SetAsync("todos/set/setAsync", new Todo
            {
                name = "Execute SET",
                priority = 2
            });

            var todoToUpdate = new Todo
            {
                name = "Execute UPDATE!",
                priority = 1
            };

            var response = await _client.UpdateAsync("todos/set/setAsync", todoToUpdate);
            Assert.NotNull(response);
            var actual = response.ResultAs<Todo>();
            Assert.AreEqual(todoToUpdate.name, actual.name);
            Assert.AreEqual(todoToUpdate.priority, actual.priority);
        }

        [Test, ExpectedException(typeof(FirebaseException)), Category("INTEGRATION"), Category("SYNC")]
        public void UpdateFailure()
        {
            var response = _client.Update("todos", true);
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async void UpdateFailureAsync()
        {
            await AssertExtensions.ThrowsAsync<FirebaseException>(async () =>
            {
                var response = await _client.UpdateAsync("todos", true);
            });
        }

        [Test, Category("INTEGRATION"), Category("ASYNC")]
        public async void GetWithQueryAsync()
        {
            await _client.PushAsync("todos/get/pushAsync", new Todo
            {
                name = "Execute PUSH4GET",
                priority = 2
            });

            await _client.PushAsync("todos/get/pushAsync", new Todo
            {
                name = "You PUSH4GET",
                priority = 2
            });

            Thread.Sleep(400);

            var response = await _client.GetAsync("todos", QueryBuilder.New().OrderBy("$key").StartAt("Exe"));
            Assert.NotNull(response);
            Assert.IsTrue(response.Body.Contains("name"));
        }
    }
}