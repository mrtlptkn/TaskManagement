using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Common.Behaviors;

namespace TaskManagement.Application;

/// <summary>
/// Application katmanının IoC kayıtları.
/// MediatR (Mediator deseni), FluentValidation ve AutoMapper burada register edilir.
/// Program.cs sadece bu extension'ı çağırır; Application içindeki detaylar
/// dış katmanlara sızmaz (encapsulation / low coupling).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline behavior sırası önemlidir: Logging -> Validation -> Handler
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddAutoMapper(assembly);

        return services;
    }
}
