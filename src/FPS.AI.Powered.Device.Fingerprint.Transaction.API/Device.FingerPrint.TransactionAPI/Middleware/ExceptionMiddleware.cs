using Fanex.Logging.Extensions;

namespace Device.FingerPrint.TransactionAPI.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Fanex.Logging.ILogger _logger;
        public ExceptionMiddleware(RequestDelegate next, Fanex.Logging.ILogger logger)
        {
            _logger = logger;
            _next = next;
        }
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                var logMessage = new LogMessage(ex.Message)
                   .AddSection("Server Info", CommonInfoGetters.GetServerInfo())
                   .AddSection("Exception Info", CommonInfoGetters.GetExceptionInfo(ex))
                   .ToString();

                await _logger.ErrorAsync(logMessage);
            }
        }
    }
}
