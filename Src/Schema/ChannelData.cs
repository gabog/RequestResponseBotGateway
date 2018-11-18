using System;
using Newtonsoft.Json;

namespace Gabog.RequestResponseBotClient.Schema
{
    public class ChannelData
    {
        //Mandatory Channel Data
        [JsonProperty(PropertyName = "usId")]
        public string UsId { get; set; }

        [JsonProperty(PropertyName = "accessToken")]
        public string AccessToken { get; set; }

        //When Available Channel Data
        [JsonProperty(PropertyName = "vinNumber")]
        public string VinNumber { get; set; }

        [JsonProperty(PropertyName = "geolocation")]
        public Geolocation Geolocation { get; set; }

        //Developer Channel Data
        [JsonProperty(PropertyName = "debug")]
        public bool Debug { get; set; }
    }
}