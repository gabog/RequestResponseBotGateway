using System;
using Newtonsoft.Json;

namespace Gabog.RequestResponseBotClient.Schema
{
    public class DiagnosticsData
    {
        [JsonProperty("reconnectAndPostDuration")]
        public long ReconnectAndPostDuration { get; set; }

        [JsonProperty("getResponseDuration")]
        public long GetResponseDuration { get; set; }

        [JsonProperty("getResponseTries")]
        public int GetResponseTries { get; set; }
    }
}