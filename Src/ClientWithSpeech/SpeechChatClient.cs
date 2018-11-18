using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;

namespace Gabog.RequestResponseBotClient.ClientWithSpeech
{
    public class SpeechChatClient :ChatClient
    {
        private readonly string _speechEndpointId;
        private readonly string _speechRegion;
        private readonly string _speechSubscriptionKey;

        public SpeechChatClient(IConfiguration configuration) : base(configuration)
        {
            // Speech
            _speechSubscriptionKey = configuration.GetSection("Speech:SubscriptionKey").Value;
            _speechRegion = configuration.GetSection("Speech:Region").Value;
            _speechEndpointId = configuration.GetSection("Speech:EndpointId").Value;
        }

        protected override async Task<string> GetUtterance()
        {
            //return base.GetUtterance();

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

            return "test";
        }
    }
}
