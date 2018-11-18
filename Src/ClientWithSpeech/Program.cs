using System;
using Microsoft.Extensions.Configuration;

namespace Gabog.RequestResponseBotClient.ClientWithSpeech
{
    public class Program
    {
        static Program()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            Configuration = config.Build();
        }

        private static IConfigurationRoot Configuration { get; }

        public static void Main()
        {
            try
            {
                var chatClient = new ChatClient(Configuration);
                chatClient.StartChat().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("Done. Please enter to continue.");
                Console.ReadLine();
            }
        }
    }
}