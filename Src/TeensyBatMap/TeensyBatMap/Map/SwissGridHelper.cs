using System;

using swisstopo.geodesy.reframe;

using UniversalMapControl;
using UniversalMapControl.Projections;

namespace TeensyBatMap.Map
{
	public static class SwissGridHelper
	{
		public static Wgs84Location ToWgs84(this SwissGridLocation sg)
		{
			Reframe reframe = new Reframe();

			double height = 555;
			double x = sg.X;
			double y = sg.Y;
			try
			{
				bool outsideSwitzerland = reframe.ComputeReframe(ref x, ref y, ref height, Reframe.PlanimetricFrame.LV03_Military, Reframe.PlanimetricFrame.LV95, Reframe.AltimetricFrame.LN02, Reframe.AltimetricFrame.Ellipsoid);
				reframe.ComputeGpsref(ref x, ref y, ref height, Reframe.ProjectionChange.LV95ToETRF93Geographic);
			}
			catch (Exception ex)
			{
				return new Wgs84Location();
			}
			return new Wgs84Location(y, x);
		}

		public static SwissGridLocation ToSwissGrid(this Wgs84Location wgs84)
		{
			Reframe reframe = new Reframe();

			double height = 555;
			double longitude = wgs84.Longitude;
			double latitude = wgs84.Latitude;
			try
			{
				reframe.ComputeGpsref(ref longitude, ref latitude, ref height, Reframe.ProjectionChange.ETRF93GeographicToLV95);
				bool outsideSwitzerland = reframe.ComputeReframe(ref longitude, ref latitude, ref height, Reframe.PlanimetricFrame.LV95, Reframe.PlanimetricFrame.LV03_Military, Reframe.AltimetricFrame.Ellipsoid, Reframe.AltimetricFrame.LN02);
				return new SwissGridLocation((int)Math.Round(longitude), (int)Math.Round(latitude));
			}
			catch (Exception ex)
			{
				return new SwissGridLocation();
			}
		}
	}
}