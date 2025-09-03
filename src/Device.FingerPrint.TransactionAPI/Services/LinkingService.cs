namespace Device.FingerPrint.TransactionAPI.Services
{
    public interface ILinkingService
    {
        Task Link(string sessionKey);
    }

    public class LinkingService : ILinkingService
    {
        private readonly ILogger logger;
        private readonly IRedisService redisService;

        public LinkingService(ILogger logger, IRedisService redisService)
        {
            this.logger = logger;
            this.redisService = redisService;
        }

        public async Task Link(string sessionKey)
        {
            //try
            //{
               
            //}
            //catch (Exception ex)
            //{
            //    logger.LogError(ex, $"Error linking session {sessionKey}");
            //    throw;
            //}
        }
    }
}
