using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gabog.RequestResponseBotClient.Schema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.Extensions.Configuration;
using Activity = Microsoft.Bot.Connector.DirectLine.Activity;

namespace Gabog.RequestResponseBotClient.DirectLineGateway.Controllers
{
    [ApiController]
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class BotGatewayController : ControllerBase
    {
        private static ConcurrentDictionary<string, Queue<Activity>> _conversationsQueue = new ConcurrentDictionary<string, Queue<Activity>>();
        private readonly string _botId;
        private readonly DirectLineClient _directLineClient;

        public BotGatewayController(IConfiguration configuration, DirectLineClient directLineClient)
        {
            _botId = configuration.GetSection("BotId").Value;
            _directLineClient = directLineClient;
        }

        [HttpPost]
        [Route("initAssistant")]
        public async Task<ActionResult<BotGatewayResponse>> InitAssistantAsync([FromBody] Activity activity, CancellationToken cancellationToken = default(CancellationToken))
        {
            var ti0 = DateTime.Now;
            var conversation = await _directLineClient.Conversations.StartConversationAsync(cancellationToken);
            var elapsed = DateTime.Now.Subtract(ti0).TotalMilliseconds;

            ti0 = DateTime.Now;
            var response = await _directLineClient.Conversations.PostActivityAsync(conversation.ConversationId, activity, cancellationToken);
            elapsed = DateTime.Now.Subtract(ti0).TotalMilliseconds;

            var initResponse = new BotGatewayResponse
            {
                Activities = new List<Activity> { GetFakeActivity() },
                ConversationId = conversation.ConversationId,
                Watermark = null
            };
            return new ActionResult<BotGatewayResponse>(Ok(initResponse));
        }

        [HttpPost]
        [Route("sendActivity")]
        public async Task<ActionResult<BotGatewayResponse>> SendActivityAsync(string conversationId, string watermark, [FromBody] Activity activity, CancellationToken cancellationToken = default(CancellationToken))
        {
            //using (var webSocketClient = new WebSocket(_directLineClient.Conversations.StreamUrl))
            //{
            //    webSocketClient.OnMessage += WebSocketClient_OnMessage;

            //    // You have to specify TLS version to 1.2 or connection will be failed in handshake.
            //    webSocketClient.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            //    webSocketClient.Connect();

            //    while (true)
            //    {
            //                await directLineClient.Conversations.PostActivityAsync(conversation.ConversationId, userMessage);
            //    }

            //}

            // Reconnect and post (run them in parallel).
            var stopwatch = Stopwatch.StartNew();
            var reconnectTask = _directLineClient.Conversations.ReconnectToConversationAsync(conversationId, watermark, cancellationToken);
            var postTask = _directLineClient.Conversations.PostActivityAsync(conversationId, activity, cancellationToken);
            Task.WaitAll(reconnectTask, postTask);
            var reconnectAndPostTime = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            var responses = await GetResponses(conversationId, cancellationToken, _directLineClient, watermark);
            var getTime = stopwatch.ElapsedMilliseconds;

            responses.Diagnostics = new DiagnosticsData
            {
                PostAndReconnectTime = reconnectAndPostTime,
                GetResponseTime = getTime
            };

            return new ActionResult<BotGatewayResponse>(Ok(responses));
        }

        [HttpGet]
        [Route("getNextMessage")]
        public ActionResult<BotGatewayResponse> GetNextMessageAsync(string conversationId, string watermark, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new ActionResult<BotGatewayResponse>(Ok(GetResponses(conversationId, cancellationToken, _directLineClient, watermark)));
        }

        private async Task<BotGatewayResponse> GetResponses(string conversationId, CancellationToken cancellationToken, DirectLineClient directLineClient, string watermark = null)
        {
            // wait and send back as soon as we get at least one answer
            try
            {
                IEnumerable<Activity> activities;
                while (true)
                {
                    var activitySet = await directLineClient.Conversations.GetActivitiesAsync(conversationId, watermark, cancellationToken).ConfigureAwait(false);
                    if (activitySet != null)
                    {
                        watermark = activitySet.Watermark;

                        if (activitySet.Activities.Count > 0)
                        {
                            activities = from x in activitySet.Activities
                                where x.From.Id == _botId
                                select x;

                            break;
                        }
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
                }

                var responses = new BotGatewayResponse
                {
                    Activities = new List<Activity>(activities),
                    ConversationId = conversationId,
                    Watermark = watermark == null ? "1" : (int.Parse(watermark) + 1).ToString()
                };

                return responses;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private Activity GetFakeActivity()
        {
            return new Activity
            {
                Type = ActivityTypes.Message,
                Text = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                Speak = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                InputHint = InputHints.ExpectingInput
            };
        }
    }
}