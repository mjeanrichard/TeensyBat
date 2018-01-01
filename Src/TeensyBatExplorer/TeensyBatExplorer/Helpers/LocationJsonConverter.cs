using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UniversalMapControl.Interfaces;
using UniversalMapControl.Projections;

namespace TeensyBatExplorer.Helpers
{
    public class LocationJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is SwissGridLocation swissGridLocation)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("type");
                writer.WriteValue(nameof(SwissGridLocation));
                writer.WritePropertyName(nameof(SwissGridLocation.X));
                writer.WriteValue(swissGridLocation.X);
                writer.WritePropertyName(nameof(SwissGridLocation.Y));
                writer.WriteValue(swissGridLocation.Y);
                writer.WriteEndObject();
            } 
            else if (value is ILocation location)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("type");
                writer.WriteValue(nameof(Wgs84Location));
                writer.WritePropertyName(nameof(Wgs84Location.Latitude));
                writer.WriteValue(location.Latitude);
                writer.WritePropertyName(nameof(Wgs84Location.Longitude));
                writer.WriteValue(location.Longitude);
                writer.WriteEndObject();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (!typeof(ILocation).IsAssignableFrom(objectType))
            {
                throw new InvalidOperationException();
            }

            JToken locationToken = JToken.ReadFrom(reader);
            if (locationToken.Type != JTokenType.Object)
            {
                return new Wgs84Location();
            }

            JObject locationObject = (JObject)locationToken;
            string type = GetStringValue(locationObject, "type");
            if (!string.IsNullOrWhiteSpace(type) && type == nameof(SwissGridLocation))
            {
                int x = locationObject.GetValue(nameof(SwissGridLocation.X))?.Value<int>() ?? 0;
                int y = locationObject.GetValue(nameof(SwissGridLocation.Y))?.Value<int>() ?? 0;
                return new SwissGridLocation(x, y);
            }

            double lat = locationObject.GetValue(nameof(Wgs84Location.Latitude))?.Value<double>() ?? 0;
            double lon = locationObject.GetValue(nameof(Wgs84Location.Longitude))?.Value<double>() ?? 0;
            return new Wgs84Location(lat, lon);
        }

        private static string GetStringValue(JObject jObject, string propertyName)
        {
            JToken token = jObject.GetValue(propertyName);
            return token?.Value<string>();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(ILocation).IsAssignableFrom(objectType);
        }
    }
}