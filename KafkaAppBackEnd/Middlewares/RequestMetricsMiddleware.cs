using Prometheus;
using System.Diagnostics;

namespace KafkaAppBackEnd.Middlewares
{
    public class RequestMetricsMiddleware
    {
        private readonly RequestDelegate _next;

        private static readonly Counter TotalRequestsCounter = Metrics.CreateCounter("http_requests_total", "Total number of HTTP requests received");
        private static readonly Histogram RequestDurationHistogram = Metrics.CreateHistogram("http_request_duration_seconds_sample", "Gauge of request processing durations");
        private static readonly Counter ErrorCounter = Metrics.CreateCounter("http_requests_errors_total", "Total number of HTTP requests resulting in an error");
        private static readonly Histogram ResponseSizeHistogram = Metrics.CreateHistogram("http_response_size_bytes", "Histogram of HTTP response sizes");

        public RequestMetricsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var responseSize = 0L;

            try
            {
                TotalRequestsCounter.Inc();

                // Hook into the response body
                var originalBodyStream = context.Response.Body;
                using (var newBodyStream = new MemoryStream())
                {
                    context.Response.Body = newBodyStream;

                    await _next(context);

                    stopwatch.Stop();
                    RequestDurationHistogram.Observe(stopwatch.Elapsed.TotalSeconds);

                    responseSize = newBodyStream.Length;
                    ResponseSizeHistogram.Observe(responseSize);

                    if (context.Response.StatusCode >= 400)
                    {
                        ErrorCounter.Inc();
                    }

                    // Copy the new body stream back to the original
                    newBodyStream.Seek(0, SeekOrigin.Begin);
                    await newBodyStream.CopyToAsync(originalBodyStream);
                }
            }
            catch
            {
                ErrorCounter.Inc();
                throw;
            }
        }
    }
}
