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

        public static void WriteFlowerLine()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\r\n=> {0:MM/dd/yyyy hh:mm:ss.fff tt} {1}", DateTime.Now, new string('*', 80));
            Console.ResetColor();
        }
    }
}