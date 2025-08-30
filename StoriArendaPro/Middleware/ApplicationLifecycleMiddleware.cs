using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace StoriArendaPro.Middleware
{
    public class ApplicationLifecycleMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApplicationLifecycleMiddleware> _logger;

        public ApplicationLifecycleMiddleware(RequestDelegate next, ILogger<ApplicationLifecycleMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Необработанное исключение в middleware");
                throw;
            }
        }
    }
}
