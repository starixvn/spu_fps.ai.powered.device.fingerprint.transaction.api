using Device.FingerPrint.TransactionAPI.Services;
using MassTransit;

namespace Device.FingerPrint.TransactionAPI.Queue
{
    public interface IMassTransitProducer
    {
        Task SendAsync<T>(T data, string address);

        Task SendBatchAsync<T>(IEnumerable<T> data, string address) where T : class;

        void Send<T>(T data, string address);
    }
    public class MassTransitProducer : IMassTransitProducer
    {
        private readonly IBus bus;
        private readonly IRedisMultiplexDbService redisMultiplexDbService;
        private readonly ILogger logger;

        public MassTransitProducer(IBus bus, IRedisMultiplexDbService redisMultiplexDbService, ILogger logger)
        {
            this.bus = bus;
            this.redisMultiplexDbService = redisMultiplexDbService;
            this.logger = logger;
        }
        public void Send<T>(T data, string address)
        {
            var endpointTask = bus.GetSendEndpoint(new Uri(address));

            endpointTask.ContinueWith(endpoint =>
            {
                if (endpoint.Status == TaskStatus.RanToCompletion)
                {
                    endpoint.Result.Send(data, new CancellationToken());
                }
            });
        }
        public async Task SendAsync<T>(T data, string address)
        {
            try
            {
                await redisMultiplexDbService.DefaultDatabase.HashIncrementAsync("TransactionAPI", "ToQueueTransaction", 1);
            }
            catch (Exception ex)
            {
                await logger.ErrorAsync("Error in Counter Insert Transaction APFP", ex);
            }

            var endpoint = await bus.GetSendEndpoint(new Uri(address));

            await endpoint.Send(data, new CancellationToken());
        }

        public async Task SendBatchAsync<T>(IEnumerable<T> data, string address) where T : class
        {
            var endpoint = await bus.GetSendEndpoint(new Uri(address));
            await endpoint.SendBatch(data, new CancellationToken());
        }
    }
}
