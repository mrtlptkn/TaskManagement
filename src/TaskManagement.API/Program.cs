using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using TaskManagement.API.Middlewares;
using TaskManagement.Application;
using TaskManagement.Application.Features.Tasks.Commands.AddActivityLog;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Seed;

// Serilog'u uygulama başlamadan önce bootstrap logger ile kur.
// Startup hatalarını da yakalar.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("TaskManagement.API başlatılıyor...");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog'u appsettings.json konfigürasyonundan oku
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    // --- Katman bazlı IoC kayıtları (Clean Architecture) ---
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

  

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    // --- Swagger / OpenAPI ---
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Task Management API",
            Version = "v1",
            Description = "Clean Architecture + Hexagonal prensipleriyle geliştirilmiş, " +
                           "Mediator (CQRS) tabanlı görev yönetimi API'si."
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT token'ı 'Bearer {token}' formatında giriniz."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // --- Veritabanı migration + seed ---
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (dbContext.Database.IsRelational())
            await dbContext.Database.MigrateAsync();
        else
            await dbContext.Database.EnsureCreatedAsync();

        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
        await TaskSeeder.SeedAsync(scope.ServiceProvider);
    }

    // --- Middleware pipeline ---
    app.UseExceptionHandling();

    // Her HTTP isteğini Serilog ile logla
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0000} ms)";
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Management API v1");
        });
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "TaskManagement.API beklenmedik hata ile durdu.");
}
finally
{
    Log.CloseAndFlush();
}

// Integration testlerde WebApplicationFactory<Program> kullanımı için partial class.
public partial class Program { }
