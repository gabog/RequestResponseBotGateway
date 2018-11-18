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
}