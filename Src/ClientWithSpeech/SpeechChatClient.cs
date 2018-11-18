using System;
using System.Threading.Tasks;
using Gabog.RequestResponseBotClient.ClientWithSpeech.Util;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Gabog.RequestResponseBotClient.ClientWithSpeech
{
    public class SpeechChatClient : ChatClient
    {
        private readonly SpeechConfig _speechConfig;

        public SpeechChatClient(IConfiguration configuration) : base(configuration)
        {
            // Speech
            var speechSubscriptionKey = configuration.GetSection("Speech:SubscriptionKey").Value;
            var speechRegion = configuration.GetSection("Speech:Region").Value;
            var speechEndpointId = configuration.GetSection("Speech:EndpointId").Value;
            _speechConfig = SpeechConfig.FromSubscription(speechSubscriptionKey, speechRegion);
            _speechConfig.SpeechRecognitionLanguage = configuration.GetSection("Speech:Locale").Value;
            if (!string.IsNullOrEmpty(speechEndpointId))
            {
                // Custom speech will use a custom endpoint ID
                _speechConfig.EndpointId = speechEndpointId;
            }
        }

        protected override async Task<string> GetUtterance()
        {
            const string saySomethingOrSayQuitToEnd = "Say something... (or say quit to end)";
            using (var recognizer = new SpeechRecognizer(_speechConfig))
            {
                recognizer.Recognizing += Recognizer_Recognizing;
                recognizer.Recognized += Recognizer_Recognized;

                while (true)
                {
                    Console.CursorLeft = 0;
                    Console.Write(saySomethingOrSayQuitToEnd);

                    // Performs recognition. RecognizeOnceAsync() returns when the first utterance has been recognized,
                    // so it is suitable only for single shot recognition like command or query. For long-running
                    // recognition, use StartContinuousRecognitionAsync() instead.
                    var result = await recognizer.RecognizeOnceAsync();

                    switch (result.Reason)
                    {
                        // Checks result.
                        case ResultReason.RecognizedSpeech:
                            var resultText = StripEndingCharacters(result);
                            ConsoleOut.WriteFinalSpeechResult(resultText);
                            return resultText.Trim();
                        case ResultReason.NoMatch:
                            break;
                        case ResultReason.Canceled:
                        {
                            var cancellation = CancellationDetails.FromResult(result);
                            Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                            if (cancellation.Reason == CancellationReason.Error)
                            {
                                Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                                Console.WriteLine("CANCELED: Did you update the subscription info?");
                            }

                            break;
                        }
                    }
                }
            }
        }

        private static string StripEndingCharacters(SpeechRecognitionResult result)
        {
            var resultText = result.Text;
            if (resultText.EndsWith('.') || resultText.EndsWith(','))
            {
                resultText = resultText.Remove(resultText.Length - 1);
            }

            return resultText;
        }

        private void Recognizer_Recognizing(object sender, SpeechRecognitionEventArgs e)
        {
            ConsoleOut.WriteInProgressSpeechResult(e.Result.Text);
        }

        private void Recognizer_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            ConsoleOut.WriteInProgressSpeechResult(e.Result.Text);
        }
    }
}