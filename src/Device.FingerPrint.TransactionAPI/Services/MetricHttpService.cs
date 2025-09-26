using Device.FingerPrint.Models;
using Prometheus;
using System.Net.Http.Headers;
using System.Text;

namespace Device.FingerPrint.TransactionAPI.Services
{
    public static class MetricHttpService
    {
        public static void PublicDataToGateway(GatewayInfo gatewayInfo)
        {
            var headerValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{gatewayInfo.Username}:{gatewayInfo.Password}"));
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", headerValue);

            var pusher = new MetricPusher(new MetricPusherOptions
            {
                Endpoint = gatewayInfo.Url,
                Job = gatewayInfo.Job,
                Instance = Environment.MachineName,
                HttpClientProvider = () => httpClient
            });

            pusher.Start();
        }
    }
}
