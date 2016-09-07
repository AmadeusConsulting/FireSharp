using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FireSharp.Config;
using FireSharp.GeoFire;
using FireSharp.Interfaces;

using NUnit.Framework;

namespace FireSharp.Tests
{
    [TestFixture]
    public class GeoHashingTests : FiresharpTestsBase
    {
        private readonly double[] _batteryParkLocation = { 40.702665, -74.016370 };

        private readonly double[] _statueOfLibertyLocation =  { 40.689217, -74.044500 };

        private readonly double[] _libertyBellLocation =  { 39.949590, -75.150270 };

        private readonly double[] _timesSquareLocation = { 40.758824, -73.985123 };

        private readonly double[] _empireStateBuildingLocation = { 40.748350, -73.985490 };

        private GeographyPoint _searchCenter;

        private GeoHashingService _geohashingService; 

        [SetUp]
        public void SetUp()
        {
            _searchCenter = new GeographyPoint
                                {
                                    // 1 new york plaza
                                    Latitude = 40.702079,
                                    Longitude = -74.011909
                                };

            if (FirebaseClient != null)
            {
                _geohashingService = new GeoHashingService(FirebaseClient, "points-of-interest", Config);

                var tasks = new List<Task>
                            {
                                _geohashingService.SetLocationAsync("BatteryPark", _batteryParkLocation[0], _batteryParkLocation[1]),
                                _geohashingService.SetLocationAsync("StatueOfLiberty", _statueOfLibertyLocation[0], _statueOfLibertyLocation[1]),
                                _geohashingService.SetLocationAsync("LibertyBell", _libertyBellLocation[0], _libertyBellLocation[1]),
                                _geohashingService.SetLocationAsync("TimesSquare", _timesSquareLocation[0], _timesSquareLocation[1]),
                                _geohashingService.SetLocationAsync(
                                    "EmpireStateBuilding",
                                    _empireStateBuildingLocation[0],
                                    _empireStateBuildingLocation[1])
                            };


                Task.WhenAll(tasks).Wait(); 
            }
        }

        [Test]
        public async void FindsLocationsWithin5Km()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            var found = await _geohashingService.FindLocationsAsync(_searchCenter, 5.0);

            Debug.WriteLine(found);
            Assert.AreEqual(2, found.Count);
            Assert.IsTrue(found.Keys.Contains("BatteryPark"));
            Assert.IsTrue(found.Keys.Contains("StatueOfLiberty"));
        }

        [Test]
        public async void FindsLocationsWithin10Km()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            var found = await _geohashingService.FindLocationsAsync(_searchCenter, 10.0);

            Assert.AreEqual(4, found.Count);
            Assert.IsTrue(found.Keys.Contains("BatteryPark"));
            Assert.IsTrue(found.Keys.Contains("StatueOfLiberty"));
            Assert.IsTrue(found.Keys.Contains("TimesSquare"));
            Assert.IsTrue(found.Keys.Contains("EmpireStateBuilding"));
        }

        [Test]
        public async void FindsLocationsWithin200Km()
        {
            if (FirebaseClient == null)
            {
                Assert.Inconclusive();
            }

            var found = await _geohashingService.FindLocationsAsync(_searchCenter, 200.0);

            Assert.AreEqual(5, found.Count);
            Assert.IsTrue(found.Keys.Contains("BatteryPark"));
            Assert.IsTrue(found.Keys.Contains("StatueOfLiberty"));
            Assert.IsTrue(found.Keys.Contains("TimesSquare"));
            Assert.IsTrue(found.Keys.Contains("EmpireStateBuilding"));
            Assert.IsTrue(found.Keys.Contains("LibertyBell"));
        }

        protected override void SetUpUniqueFirebaseUrlPath()
        {
            // no unique path needed for locations (in fact, we need a specific location that has indexing configured)
        }
    }
}
