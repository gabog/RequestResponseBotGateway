using System;
using System.Collections.Generic;
using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;

namespace Gabog.RequestResponseBotClient.Schema
{
    // TODO: Find a solution so we can use Schema in BF and not in DL.
    public class BotGatewayResponse
    {
        [JsonProperty("activity")]
        public IList<Activity> Activities { get; set; }

        [JsonProperty("watermark")]
        public string Watermark { get; set; }

        [JsonProperty("conversationId")]
        public string ConversationId { get; set; }

        [JsonProperty("diagnostics")]
        public DiagnosticsData Diagnostics { get; set; }
    }

    public class DiagnosticsData
    {
        public long PostAndReconnectTime { get; set; }
        public long GetResponseTime { get; set; }
    }

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

    public class Geolocation
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}