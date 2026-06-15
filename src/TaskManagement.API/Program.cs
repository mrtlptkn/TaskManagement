using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TaskManagement.API.Middlewares;
using TaskManagement.Application;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

// --- Katman bazlı IoC kayıtları (Clean Architecture) ---
// API katmanı sadece Application ve Infrastructure'ın DI extension'larını çağırır;
// Application/Infrastructure detaylarını bilmez.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enum'ların (TaskPriority, TaskStatusEnum vb.) JSON'da string olarak
        // ("Urgent", "InProgress" vb.) serialize/deserialize edilmesini sağlar.
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

    // JWT Bearer için Swagger UI desteği
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

// --- Veritabanı migration + seed (development kolaylığı) ---
// InMemory provider (integration testlerde kullanılır) migration'ı desteklemez,
// bu yüzden relational provider kontrolü yapılır.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
    }
    else
    {
        await dbContext.Database.EnsureCreatedAsync();
    }

    // Identity seed (kullanıcılar ve roller)
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);

    // Task seed (demo görevleri)
    await TaskSeeder.SeedAsync(scope.ServiceProvider);
}

// --- Middleware pipeline ---
app.UseExceptionHandling();

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

// Integration testlerde WebApplicationFactory<Program> kullanımı için partial class.
public partial class Program { }
