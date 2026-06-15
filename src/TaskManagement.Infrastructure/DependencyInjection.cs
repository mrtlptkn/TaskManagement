using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Authorization;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Repositories;
using TaskManagement.Infrastructure.Services;

namespace TaskManagement.Infrastructure;

/// <summary>
/// Infrastructure katmanının IoC kayıtları.
/// Application katmanında tanımlanan port'ların (interface) somut
/// implementasyonları (adapter) burada DI container'a bağlanır.
/// Bu sayede Application/Domain hiçbir zaman EF Core, Identity veya JWT'ye bağımlı olmaz.
/// </summary>
public static class DependencyInjection
{
  public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
  {
    // --- Persistence ---
    services.AddDbContext<ApplicationDbContext>(options =>

    {
      options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
      options.EnableSensitiveDataLogging();
      options.LogTo(Console.WriteLine, LogLevel.Information);
    });


    services.AddScoped<ITaskRepository, TaskRepository>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();

    // --- Identity ---
    services.AddIdentity<AppUser, AppRole>(options =>
        {
          options.Password.RequiredLength = 6;
          options.Password.RequireNonAlphanumeric = false;
          options.Password.RequireUppercase = false;
          options.Password.RequireDigit = false;
          options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

    // --- JWT Authentication ---
    services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
    var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
        ?? throw new InvalidOperationException("JwtSettings yapılandırması bulunamadı.");

    services.AddAuthentication(options =>
        {
          options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
          options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
          options.TokenValidationParameters = new TokenValidationParameters
          {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
          };
        });

    // --- Authorization (Resource Based) ---
    services.AddAuthorization(options =>
    {
      options.AddPolicy("TaskOwnerPolicy", policy =>
              policy.Requirements.Add(new TaskOwnerRequirement()));
    });

    services.AddScoped<IAuthorizationHandler, TaskOwnerAuthorizationHandler>();

    // --- Application Services (Adapters) ---
    services.AddHttpContextAccessor();
    services.AddScoped<ICurrentUserService, CurrentUserService>();
    services.AddScoped<IIdentityService, IdentityService>();
    services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
    services.AddSingleton<IFileStorageService>(_ => new LocalFileStorageService());

    return services;
  }
}
