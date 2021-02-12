// 
// Teensy Bat Explorer - Copyright(C) 2020 Meinrad Jean-Richard
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Globalization;

namespace TeensyBatExplorer.Core.Maps
{
    public class SwissGridLocation
    {
        public static SwissGridLocation FromWgs84Approx(Wgs84Location location)
        {
            return FromWgs84Approx(location.Latitude, location.Longitude);
        }

        public static SwissGridLocation FromWgs84Approx(double latitude, double longitude)
        {
            double x = WGStoCHy(latitude, longitude);
            double y = WGStoCHx(latitude, longitude);
            SwissGridLocation sg = new(x, y);
            return sg;
        }

        /// <summary>
        /// Convert WGS lat/long (° dec) to CH y
        /// </summary>
        private static double WGStoCHy(double lat, double lng)
        {
            // Convert decimal degrees to sexagesimal seconds
            lat = DecToSexAngle(lat);
            lng = DecToSexAngle(lng);

            // Auxiliary values (% Bern)
            double latAux = (lat - 169028.66) / 10000.0;
            double lngAux = (lng - 26782.5) / 10000.0;

            // Process Y
            double y = 600072.37
                       + 211455.93 * lngAux
                       - 10938.51 * lngAux * latAux
                       - 0.36 * lngAux * Math.Pow(latAux, 2)
                       - 44.54 * Math.Pow(lngAux, 3);

            return y;
        }

        /// <summary>
        /// Convert WGS lat/long (° dec) to CH x
        /// </summary>
        private static double WGStoCHx(double lat, double lng)
        {
            // Convert decimal degrees to sexagesimal seconds
            lat = DecToSexAngle(lat);
            lng = DecToSexAngle(lng);

            // Auxiliary values (% Bern)
            double latAux = (lat - 169028.66) / 10000.0;
            double lngAux = (lng - 26782.5) / 10000.0;

            // Process X
            double x = 200147.07
                       + 308807.95 * latAux
                       + 3745.25 * Math.Pow(lngAux, 2)
                       + 76.63 * Math.Pow(latAux, 2)
                       - 194.56 * Math.Pow(lngAux, 2) * latAux
                       + 119.79 * Math.Pow(latAux, 3);

            return x;
        }

        /// <summary>
        /// Convert WGS lat/long (° dec) and height to CH h
        /// </summary>
        private static double WGStoCHh(double lat, double lng, double h)
        {
            // Convert decimal degrees to sexagesimal seconds
            lat = DecToSexAngle(lat);
            lng = DecToSexAngle(lng);

            // Auxiliary values (% Bern)
            double latAux = (lat - 169028.66) / 10000.0;
            double lngAux = (lng - 26782.5) / 10000.0;

            // Process h
            h = h - 49.55
                + 2.73 * lngAux
                + 6.94 * latAux;

            return h;
        }

        /// <summary>
        /// Convert CH y/x to WGS lat
        /// </summary>
        private static double CHtoWGSlat(double y, double x)
        {
            // Converts military to civil and  to unit = 1000km
            // Auxiliary values (% Bern)
            double yAux = (y - 600000.0) / 1000000.0;
            double xAux = (x - 200000.0) / 1000000.0;

            // Process lat
            double lat = 16.9023892
                         + 3.238272 * xAux
                         - 0.270978 * Math.Pow(yAux, 2)
                         - 0.002528 * Math.Pow(xAux, 2)
                         - 0.0447 * Math.Pow(yAux, 2) * xAux
                         - 0.0140 * Math.Pow(xAux, 3);

            // Unit 10000" to 1 " and converts seconds to degrees (dec)
            lat = lat * 100 / 36;

            return lat;
        }

        /// <summary>
        /// Convert CH y/x to WGS long
        /// </summary>
        private static double CHtoWGSlng(double y, double x)
        {
            // Converts military to civil and  to unit = 1000km
            // Auxiliary values (% Bern)
            double yAux = (y - 600000.0) / 1000000.0;
            double xAux = (x - 200000.0) / 1000000.0;

            // Process long
            double lng = 2.6779094
                         + 4.728982 * yAux
                         + 0.791484 * yAux * xAux
                         + 0.1306 * yAux * Math.Pow(xAux, 2)
                         - 0.0436 * Math.Pow(yAux, 3);

            // Unit 10000" to 1 " and converts seconds to degrees (dec)
            lng = lng * 100.0 / 36.0;

            return lng;
        }

        /// <summary>
        /// Convert CH y/x/h to WGS height
        /// </summary>
        private static double CHtoWGSheight(double y, double x, double h)
        {
            // Converts military to civil and  to unit = 1000km
            // Auxiliary values (% Bern)
            double yAux = (y - 600000.0) / 1000000.0;
            double xAux = (x - 200000.0) / 1000000.0;

            // Process height
            h = h + 49.55 - 12.60 * yAux - 22.64 * xAux;

            return h;
        }

        /// <summary>
        /// Convert angle in decimal degrees to sexagesimal seconds
        /// </summary>
        public static double DecToSexAngle(double dec)
        {
            int deg = (int)Math.Floor(dec);
            int min = (int)Math.Floor((dec - deg) * 60.0);
            double sec = ((dec - deg) * 60.0 - min) * 60.0;

            return sec + min * 60.0 + deg * 3600.0;
        }

        private double _longitude;
        private double _latitude;

        public SwissGridLocation() : this(600000, 200000)
        {
        }

        public SwissGridLocation(int x, int y) : this(x, (double)y)
        {
        }

        public SwissGridLocation(double x, double y)
        {
            X = x;
            Y = y;
            _latitude = CHtoWGSlat(x, y);
            _longitude = CHtoWGSlng(x, y);
        }

        public double X { get; private set; }
        public double Y { get; private set; }

        public Wgs84Location ToWgs84Approx()
        {
            double lat = CHtoWGSlat(X, Y);
            double lon = CHtoWGSlng(X, Y);
            Wgs84Location location = new(lat, lon);
            return location;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} // {1}", X, Y);
        }
    }
}