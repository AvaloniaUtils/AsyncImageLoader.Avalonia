using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Memory.Services;

namespace AsyncImageLoader.DevTools;

public class LoggingHandler : DelegatingHandler
{
    public LoggingHandler() : base(new HttpClientHandler())
    {
        
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if(!HttpClientMonitor.Instance.IsRunning)
            return await base.SendAsync(request, cancellationToken);
        
        var log = new HttpRequestLog
        {
            Url = request.RequestUri?.ToString() ?? "",
            Method = request.Method,
            Timestamp = DateTime.UtcNow
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();
            log.StatusCode = (int)response.StatusCode;
            log.Duration = sw.Elapsed;
            
            HttpClientMonitor.Instance.Log(log);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            log.Exception = ex;
            log.Duration = sw.Elapsed;
            
            HttpClientMonitor.Instance.Log(log);
            throw;
        }
    }
}

