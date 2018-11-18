using System;
using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;

namespace Gabog.RequestResponseBotClient.ClientWithSpeech.Util
{
    public static class ConsoleOut
    {
        public static void RenderActivity(Activity activity, ConsoleColor foregroundColor)
        {
            Console.ForegroundColor = foregroundColor;
            var s = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };

            var formatted = JsonConvert.SerializeObject(activity, s);
            Console.WriteLine(formatted);
            Console.ResetColor();
        }

        public static void WriteTiming(string message, long duration)
        {
            Console.Write(message);
            Console.Write(" ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("{0:N0}", duration);
            Console.ResetColor();
        }

        public static void WriteFlowerLine()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\r\n=> {0:MM/dd/yyyy hh:mm:ss.fff tt} {1}", DateTime.Now, new string('*', 80));
            Console.ResetColor();
        }

        public static void WriteFinalSpeechResult(string text)
        {
            WriteSpeechResult(text, ConsoleColor.DarkCyan, ConsoleColor.Black);
            Console.WriteLine();
        }

        public static void WriteInProgressSpeechResult(string text)
        {
            WriteSpeechResult(text, ConsoleColor.DarkRed, ConsoleColor.Black);
        }

        private static void WriteSpeechResult(string text, ConsoleColor backgroundColor, ConsoleColor foregroundColor)
        {
            // Clear previous text
            Console.CursorLeft = 0;
            Console.Write(new string(' ', Console.BufferWidth - 1));

            // Output text
            Console.BackgroundColor = backgroundColor;
            Console.ForegroundColor = foregroundColor;
            Console.CursorLeft = 0;
            Console.Write(text);
            Console.ResetColor();
        }
    }
}