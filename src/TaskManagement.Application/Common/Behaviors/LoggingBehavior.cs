using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace TaskManagement.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior: her Command/Query çağrısını ve süresini loglar.
/// Cross-cutting concern olarak handler'lara karışmaz (SRP).
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("İşlem başladı: {RequestName}", requestName);

        try
        {
            var response = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "İşlem tamamlandı: {RequestName} ({ElapsedMilliseconds} ms)",
                requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "İşlem hata ile sonuçlandı: {RequestName} ({ElapsedMilliseconds} ms)",
                requestName, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
