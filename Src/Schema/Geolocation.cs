using System;
using Newtonsoft.Json;

namespace Gabog.RequestResponseBotClient.Schema
{
    public class Geolocation
    {
        [JsonProperty("latitude")]
        public double? Latitude { get; set; }

        [JsonProperty("longitude")]
        public double? Longitude { get; set; }
    }
}