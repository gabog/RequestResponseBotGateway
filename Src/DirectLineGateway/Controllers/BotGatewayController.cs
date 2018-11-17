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
            // TODO: Init shouldn't return a response, this seems to be a bug.
            var r = await GetResponses(conversation.ConversationId, null, _directLineClient, cancellationToken);

            var initResponse = new BotGatewayResponse
            {
                Activities = new List<Activity> { GetFakeActivity() },
                ConversationId = conversation.ConversationId,
                Watermark = r.Watermark
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

            // TODO: how do we check error handling if we don't await...
            var postTask = _directLineClient.Conversations.PostActivityAsync(conversationId, activity, cancellationToken);

            Task.WaitAll(reconnectTask);
            var reconnectAndPostTime = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            var responses = await GetResponses(conversationId, watermark, _directLineClient, cancellationToken);
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
        public async Task<ActionResult<BotGatewayResponse>> GetNextMessageAsync(string conversationId, string watermark, CancellationToken cancellationToken = default(CancellationToken))
        {
            var stopwatch = Stopwatch.StartNew();
            await _directLineClient.Conversations.ReconnectToConversationAsync(conversationId, watermark, cancellationToken);
            var reconnectAndPostTime = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            var response = await GetResponses(conversationId, watermark, _directLineClient, cancellationToken);
            var getTime = stopwatch.ElapsedMilliseconds;

            response.Diagnostics = new DiagnosticsData
            {
                PostAndReconnectTime = reconnectAndPostTime,
                GetResponseTime = getTime
            };

            return new ActionResult<BotGatewayResponse>(Ok(response));
        }

        private async Task<BotGatewayResponse> GetResponses(string conversationId, string watermark, DirectLineClient directLineClient, CancellationToken cancellationToken)
        {
            // wait and send back as soon as we get at least one answer
            try
            {
                List<Activity> activities;
                while (true)
                {
                    var activitySet = await directLineClient.Conversations.GetActivitiesAsync(conversationId, watermark, cancellationToken);
                    if (activitySet != null)
                    {
                        watermark = activitySet.Watermark;

                        if (activitySet.Activities.Count > 0)
                        {
                            var receivedActivities = activitySet.Activities.Where(a => a.From.Id == _botId).ToList();

                            if (receivedActivities.Any())
                            {
                                activities = receivedActivities;
                                break;
                            }
                        }
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
                }

                var responses = new BotGatewayResponse
                {
                    Activities = activities,
                    ConversationId = conversationId,
                    Watermark = watermark
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