using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace StoriArendaPro.Middleware
{
    public class MemoryMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MemoryMonitoringMiddleware> _logger;

        public MemoryMonitoringMiddleware(RequestDelegate next, ILogger<MemoryMonitoringMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var memoryUsage = GC.GetTotalMemory(false) / 1024 / 1024;
            if (memoryUsage > 500) // 500MB
            {
                _logger.LogWarning("Высокое использование памяти: {MemoryUsage}MB", memoryUsage);
                GC.Collect();
            }

            await _next(context);
        }
    }
}
