using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Gabog.RequestResponseBotClient.ClientWithSpeech.ServiceClients;
using Gabog.RequestResponseBotClient.ClientWithSpeech.Util;
using Gabog.RequestResponseBotClient.Schema;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Activity = Microsoft.Bot.Connector.DirectLine.Activity;

namespace Gabog.RequestResponseBotClient.ClientWithSpeech
{
    public class Program
    {
        private static readonly string _fromId;
        private static readonly string _locale = "en-US";
        private static readonly string _gatewayUrl;
        private static readonly bool _renderOutgoingActivities;
        private static readonly bool _renderIncomingActivities;
        private static readonly string _speechSubscriptionKey;
        private static readonly string _speechRegion;
        private static readonly string _speechEndpointId;

        static Program()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            Configuration = config.Build();

            _gatewayUrl = Configuration.GetSection("GatewayUrl").Value;
            _fromId = Configuration.GetSection("FromId").Value;
            _renderOutgoingActivities = bool.Parse(Configuration.GetSection("RenderOutgoingActivities").Value);
            _renderIncomingActivities = bool.Parse(Configuration.GetSection("RenderIncomingActivities").Value);

            // Speech
            _speechSubscriptionKey = Configuration.GetSection("Speech:SubscriptionKey").Value;
            _speechRegion = Configuration.GetSection("Speech:Region").Value;
            _speechEndpointId = Configuration.GetSection("Speech:EndpointId").Value;
        }

        private static IConfigurationRoot Configuration { get; }

        public static void Main()
        {
            try
            {
                DoTextChat().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            //DoSpeechChat().Wait();
            Console.WriteLine("Done. Please enter to continue.");
            Console.ReadLine();
        }

        public static async Task DoSpeechChat()
        {
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key // and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription(_speechSubscriptionKey, _speechRegion);
            if (!string.IsNullOrEmpty(_speechEndpointId))
            {
                // Custom speech will use a custom endpoint ID
                config.EndpointId = _speechEndpointId;
            }

            // Creates a speech recognizer.
            using (var recognizer = new SpeechRecognizer(config))
            {
                var quit = false;
                while (!quit)
                {
                    Console.WriteLine("Say something... (or say quit to end)");

                    // Performs recognition. RecognizeOnceAsync() returns when the first utterance has been recognized,
                    // so it is suitable only for single shot recognition like command or query. For long-running
                    // recognition, use StartContinuousRecognitionAsync() instead.
                    var result = await recognizer.RecognizeOnceAsync();

                    // Checks result.
                    if (result.Reason == ResultReason.RecognizedSpeech)
                    {
                        Console.WriteLine($"We recognized: {result.Text}");
                        if (result.Text.ToLower().Trim().StartsWith("quit"))
                        {
                            quit = true;
                        }
                    }
                    else if (result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine("NOMATCH: Speech could not be recognized.");
                    }
                    else if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = CancellationDetails.FromResult(result);
                        Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                            Console.WriteLine("CANCELED: Did you update the subscription info?");
                        }
                    }
                }
            }
        }

        public static async Task DoTextChat(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var dlGatewayProxy = new DirectLineGatewayProxy(new Uri(_gatewayUrl)))
            {
                var initActivity = GetInitActivity(new ChannelAccount(_fromId), _locale, GetChannelData(Configuration));

                var stopWatch = Stopwatch.StartNew();
                Console.WriteLine("Initializing...");
                var lastResponse = await dlGatewayProxy.InitAssistantAsync(initActivity, cancellationToken);
                Console.WriteLine(stopWatch.ElapsedMilliseconds);

                var quit = false;
                while (!quit)
                {
                    Console.WriteLine("Type something... (or type quit to end)");
                    var utterance = Console.ReadLine();

                    if (string.IsNullOrEmpty(utterance))
                    {
                        continue;
                    }

                    if (utterance.ToLower().Trim().StartsWith("quit"))
                    {
                        quit = true;
                    }
                    else
                    {
                        ConsoleOut.WriteFlowerLine();
                        Console.WriteLine($"Utterance: \"{utterance}\"");
                        stopWatch.Restart();
                        lastResponse = await HandleUtterance(lastResponse.ConversationId, utterance, lastResponse.Watermark, dlGatewayProxy, cancellationToken);
                        Console.WriteLine($"Total time: {stopWatch.ElapsedMilliseconds}");
                    }
                }
            }
        }

        private static async Task<BotGatewayResponse> HandleUtterance(string conversationId, string utterance, string watermark, DirectLineGatewayProxy dlGatewayProxy, CancellationToken cancellationToken)
        {
            var activity = BuildMessageActivity(new ChannelAccount(_fromId), utterance, _locale, GetChannelData(Configuration));
            var lastResponse = await SendActivityAsync(conversationId, watermark, dlGatewayProxy, cancellationToken, activity);
            var lastActivity = lastResponse.Activities[lastResponse.Activities.Count - 1];

            while (lastActivity.InputHint != InputHints.AcceptingInput && lastActivity.InputHint != InputHints.ExpectingInput)
            {
                RenderResponses(lastResponse.Activities);
                BotGatewayResponse nextResponse;
                do
                {
                    // poll for next message until we get a new message
                    Console.WriteLine("Polling for next message...");
                    var stopWatch = Stopwatch.StartNew();
                    nextResponse = await dlGatewayProxy.GetNextMessageAsync(conversationId, lastResponse.Watermark, cancellationToken);
                    var elapsedMilliseconds = stopWatch.ElapsedMilliseconds;
                    Console.WriteLine("Done.");
                    RenderResponse(nextResponse, elapsedMilliseconds);
                } while (nextResponse == null);

                lastResponse = nextResponse;
                lastActivity = lastResponse.Activities[lastResponse.Activities.Count - 1];
            }

            RenderResponses(lastResponse.Activities);
            return lastResponse;
        }

        private static async Task<BotGatewayResponse> SendActivityAsync(string conversationId, string watermark, DirectLineGatewayProxy dlGatewayProxy, CancellationToken cancellationToken, Activity activity)
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

        private static void RenderResponse(BotGatewayResponse lastResponse, long clientRoundtripTime)
        {
            Console.WriteLine($"\tClient roundtrip duration (ms): {clientRoundtripTime:N0}");
            Console.WriteLine($"\tActivities received: {lastResponse.Activities.Count}");
            Console.WriteLine($"\tWatermark: {lastResponse.Watermark}");
            Console.WriteLine($"\tServer: Reconnect and post duration (ms): {lastResponse.Diagnostics.PostAndReconnectTime:N0}");
            Console.WriteLine($"\tServer: Get response duration (ms): {lastResponse.Diagnostics.GetResponseTime:N0}");
        }

        private static void RenderResponses(IList<Activity> activities)
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
    }
}