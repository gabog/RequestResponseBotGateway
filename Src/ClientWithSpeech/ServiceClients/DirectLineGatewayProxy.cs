using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gabog.RequestResponseBotClient.Schema;
using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;

namespace Gabog.RequestResponseBotClient.ClientWithSpeech.ServiceClients
{
    public class DirectLineGatewayProxy : IDisposable
    {
        private readonly HttpClient _httpClient;

        public DirectLineGatewayProxy(Uri gatewayUri)
        {
            _httpClient = new HttpClient { BaseAddress = gatewayUri };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        public async Task<BotGatewayResponse> InitAssistantAsync(Activity activity, CancellationToken cancellationToken)
        {
            var httpContent = new StringContent(JsonConvert.SerializeObject(activity), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/botGateway/initAssistant", httpContent, cancellationToken);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"InitAssistantAsync failed. {content}");
            }

            return JsonConvert.DeserializeObject<BotGatewayResponse>(content);
        }

        public async Task<BotGatewayResponse> SendActivityAsync(string conversationId, string watermark, Activity activity, CancellationToken cancellationToken)
        {
            var httpContent = new StringContent(JsonConvert.SerializeObject(activity), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"api/botGateway/sendActivity/?conversationId={conversationId}&watermark={watermark}", httpContent, cancellationToken);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"SendActivityAsync failed. {content}");
            }

            return JsonConvert.DeserializeObject<BotGatewayResponse>(content);
        }

        public async Task<BotGatewayResponse> GetNextMessageAsync(string conversationId, string watermark, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync($"api/botGateway/getNextMessage/?conversationId={conversationId}&watermark={watermark}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"GetNextMessageAsync failed. {content}");
            }

            return JsonConvert.DeserializeObject<BotGatewayResponse>(content);
        }
    }
}