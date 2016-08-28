using System;

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

using TeensyBatMap.Map;

using UniversalMapControl;
using UniversalMapControl.Projections;

namespace TeensyBatMap.Tests
{
	[TestClass]
	public class SwissGridTests
	{
		[DataTestMethod]
		[DataRow(634921, 244309, 47.348712771, 7.900784851)]
		public void CanConvertToLocation(int x, int y, double latitude, double longitude)
		{
			SwissGridLocation sg = new SwissGridLocation(x, y);
			Wgs84Location location = SwissGridHelper.ToWgs84(sg);

			if (Math.Abs(latitude - location.Latitude) > 0.0000001)
			{
				Assert.Fail("Expected Value {0} for Latitude is not Eual to actual Value {1}. (Diff: {2})", latitude, location.Latitude, Math.Abs(latitude - location.Latitude));
			}
			if (Math.Abs(longitude - location.Longitude) > 0.0000001)
			{
				Assert.Fail("Expected Value {0} for Longitude is not Eual to actual Value {1}. (Diff: {2})", longitude, location.Longitude, Math.Abs(longitude - location.Longitude));
			}
		}

		[DataTestMethod]
		[DataRow(634921, 244309)]
		[DataRow(634000, 244000)]
		[DataRow(600001, 200001)]
		public void CanRoundtrip(int x, int y)
		{
			SwissGridLocation expected = new SwissGridLocation(x, y);
			Wgs84Location location = SwissGridHelper.ToWgs84(expected);
			SwissGridLocation actual = location.ToSwissGrid();

			Assert.AreEqual(expected.X, actual.X);
			Assert.AreEqual(expected.Y, actual.Y);
		}
	}
}