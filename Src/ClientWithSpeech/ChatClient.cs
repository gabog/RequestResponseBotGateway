﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Gabog.RequestResponseBotClient.ClientWithSpeech.ServiceClients;
using Gabog.RequestResponseBotClient.ClientWithSpeech.Util;
using Gabog.RequestResponseBotClient.Schema;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.Extensions.Configuration;
using Activity = Microsoft.Bot.Connector.DirectLine.Activity;

namespace Gabog.RequestResponseBotClient.ClientWithSpeech
{
    public class ChatClient
    {
        private readonly string _fromId;
        private readonly string _gatewayUrl;
        private readonly string _locale;
        private readonly bool _renderIncomingActivities;
        private readonly bool _renderOutgoingActivities;

        public ChatClient(IConfiguration configuration)
        {
            Configuration = configuration;
            _gatewayUrl = configuration.GetSection("GatewayUrl").Value;
            _fromId = configuration.GetSection("FromId").Value;
            _locale = configuration.GetSection("Locale").Value;
            _renderOutgoingActivities = bool.Parse(configuration.GetSection("RenderOutgoingActivities").Value);
            _renderIncomingActivities = bool.Parse(configuration.GetSection("RenderIncomingActivities").Value);
        }

        protected IConfiguration Configuration { get; }

        public async Task StartChat(CancellationToken cancellationToken = default(CancellationToken))
        {
            RenderSettings();
            using (var dlGatewayProxy = new DirectLineGatewayProxy(new Uri(_gatewayUrl)))
            {
                var initActivity = GetInitActivity(new ChannelAccount(_fromId), _locale, GetChannelData(Configuration));

                var stopWatch = Stopwatch.StartNew();
                Console.WriteLine("Initializing...");
                var lastResponse = await dlGatewayProxy.InitAssistantAsync(initActivity, cancellationToken);
                ConsoleOut.WriteTiming("Total time:", stopWatch.ElapsedMilliseconds);

                while (true)
                {
                    ConsoleOut.WriteFlowerLine();
                    var utterance = await GetUtterance();

                    if (string.IsNullOrEmpty(utterance))
                    {
                        continue;
                    }

                    if (utterance.ToLower().Trim().StartsWith("quit"))
                    {
                        break;
                    }

                    Console.WriteLine($"Utterance: \"{utterance}\"");
                    stopWatch.Restart();
                    lastResponse = await HandleUtterance(lastResponse.ConversationId, utterance, lastResponse.Watermark, dlGatewayProxy, cancellationToken);
                    ConsoleOut.WriteTiming("Total time:", stopWatch.ElapsedMilliseconds);
                    Console.WriteLine();
                }
            }
        }

        protected virtual Task<string> GetUtterance()
        {
            Console.WriteLine("Type something... (or type quit to end)");
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            Console.ForegroundColor = ConsoleColor.Black;
            var utterance = Console.ReadLine();
            Console.ResetColor();
            return Task.FromResult(utterance);
        }

        private async Task<BotGatewayResponse> HandleUtterance(string conversationId, string utterance, string watermark, DirectLineGatewayProxy dlGatewayProxy, CancellationToken cancellationToken)
        {
            var activity = BuildMessageActivity(new ChannelAccount(_fromId), utterance, _locale, GetChannelData(Configuration));
            var lastResponse = await SendActivityAsync(conversationId, watermark, dlGatewayProxy, cancellationToken, activity);
            var lastActivity = lastResponse.Activities[lastResponse.Activities.Count - 1];

            while (lastActivity.InputHint != InputHints.AcceptingInput && lastActivity.InputHint != InputHints.ExpectingInput)
            {
                RenderResponses(lastResponse.Activities);

                // Get next message until we get a new message
                Console.WriteLine("Getting next message...");
                var stopWatch = Stopwatch.StartNew();
                var nextResponse = await dlGatewayProxy.GetNextMessageAsync(conversationId, lastResponse.Watermark, cancellationToken);
                var elapsedMilliseconds = stopWatch.ElapsedMilliseconds;
                Console.WriteLine("Done.");
                RenderResponse(nextResponse, elapsedMilliseconds);

                lastResponse = nextResponse;
                lastActivity = lastResponse.Activities[lastResponse.Activities.Count - 1];
            }

            RenderResponses(lastResponse.Activities);
            return lastResponse;
        }

        private async Task<BotGatewayResponse> SendActivityAsync(string conversationId, string watermark, DirectLineGatewayProxy dlGatewayProxy, CancellationToken cancellationToken, Activity activity)
        {
            Console.WriteLine("Sending activity...");
            if (_renderOutgoingActivities)
            {
                ConsoleOut.RenderActivity(activity, ConsoleColor.Blue);
            }

            var stopWatch = Stopwatch.StartNew();
            var lastResponse = await dlGatewayProxy.SendActivityAsync(conversationId, watermark, activity, cancellationToken);
            var clientRoundtripTime = stopWatch.ElapsedMilliseconds;
            Console.WriteLine("Done.");
            RenderResponse(lastResponse, clientRoundtripTime);
            return lastResponse;
        }

        private static Activity GetInitActivity(ChannelAccount channelAccount, string locale, object channelData)
        {
            var activity = new Activity
            {
                From = channelAccount,
                Type = ActivityTypes.Event,
                Locale = locale,
                ChannelData = channelData
            };
            return activity;
        }

        private static Activity BuildMessageActivity(ChannelAccount channelAccount, string utterance, string locale, object channelData)
        {
            var activity = new Activity
            {
                From = channelAccount,
                Type = ActivityTypes.Message,
                Text = utterance,
                Locale = locale,
                ChannelData = channelData
            };
            return activity;
        }

        private static object GetChannelData(IConfiguration configuration)
        {
            return new ChannelData
            {
                UsId = configuration.GetSection("ChannelData:UsId").Value,
                AccessToken = configuration.GetSection("ChannelData:AccessToken").Value,
                VinNumber = configuration.GetSection("ChannelData:VinNumber").Value,
                Debug = bool.Parse(configuration.GetSection("ChannelData:Debug").Value),
                Geolocation = new Geolocation
                {
                    Latitude = double.Parse(configuration.GetSection("ChannelData:Geolocation:Latitude").Value),
                    Longitude = double.Parse(configuration.GetSection("ChannelData:Geolocation:Longitude").Value)
                }
            };
        }

        private void RenderResponses(IList<Activity> activities)
        {
            foreach (var activity in activities)
            {
                if (_renderIncomingActivities)
                {
                    ConsoleOut.RenderActivity(activity, ConsoleColor.Yellow);
                }

                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.White;
                Console.WriteLine(activity.Speak);
                Console.ResetColor();
            }
        }

        private static void RenderResponse(BotGatewayResponse lastResponse, long clientRoundtripTime)
        {
            ConsoleOut.WriteTiming("\tClient roundtrip duration (ms):", clientRoundtripTime);
            Console.WriteLine($"\tActivities received: {lastResponse.Activities.Count}");
            Console.WriteLine($"\tWatermark: {lastResponse.Watermark}");
            ConsoleOut.WriteTiming("\tServer: Reconnect (and post) duration (ms):", lastResponse.Diagnostics.ReconnectAndPostDuration);
            ConsoleOut.WriteTiming("\tServer: Get response duration (ms):", lastResponse.Diagnostics.GetResponseDuration);
            Console.WriteLine($"\tServer: Get response retries: {lastResponse.Diagnostics.GetResponseTries:N0}");
        }

        private void RenderSettings()
        {
            Console.WriteLine("Configuration settings");
            Console.WriteLine($"\tGatewayUrl: {_gatewayUrl}");
            Console.WriteLine($"\tFromId: {_fromId}");
            Console.WriteLine($"\tLocale: {_locale}");
            Console.WriteLine($"\tRender incoming activities: {_renderIncomingActivities}");
            Console.WriteLine($"\tRender outgoing activities: {_renderOutgoingActivities}");
            Console.WriteLine();
        }
    }
}