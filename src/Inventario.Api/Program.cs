using System.Text;
using Inventario.Api.Hosting;
using Inventario.Api.Http;
using Inventario.Api.Options;
using Inventario.Application;
using Inventario.Application.Options;
using Inventario.Application.Results;
using Inventario.Contracts.Auth;
using Inventario.Data.Identity;
using Inventario.Data.System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, _, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console();

        // Archivo rolling solo en Development (sin sinks remotos).
        if (context.HostingEnvironment.IsDevelopment())
        {
            configuration.WriteTo.File(
                path: "logs/inventario-api-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14);
        }
    });

    var identityConnection = builder.Configuration.GetConnectionString("Identity") ?? string.Empty;
    var systemConnection = builder.Configuration.GetConnectionString("System") ?? string.Empty;

    builder.Services.AddInventarioIdentityData(options => options.UseSqlServer(identityConnection));
    builder.Services.AddInventarioSystemData(options => options.UseSqlServer(systemConnection));

    var lockoutMax = builder.Configuration.GetValue("Identity:Lockout:MaxFailedAccessAttempts", 5);
    var lockoutMinutes = builder.Configuration.GetValue("Identity:Lockout:DefaultLockoutMinutes", 15);

    builder.Services
        .AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.User.RequireUniqueEmail = false;
            options.SignIn.RequireConfirmedAccount = false;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = lockoutMax;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(lockoutMinutes);
            // Alineado a ChangePasswordRequest / AuthValidationMessages (V2); resto = defaults Identity.
            options.Password.RequiredLength = AuthValidationMessages.PasswordMinLength;
        })
        .AddEntityFrameworkStores<InventarioIdentityDbContext>()
        .AddDefaultTokenProviders();

    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
    builder.Services.Configure<SeedAdminOptions>(builder.Configuration.GetSection(SeedAdminOptions.SectionName));
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddInventarioApplication();

    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer();

    builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<Microsoft.Extensions.Options.IOptions<JwtOptions>, IHostEnvironment>(
            (bearerOptions, jwtAccessor, environment) =>
            {
                var jwt = jwtAccessor.Value;
                // Placeholder solo para poder arrancar sin secrets; emisión real exige SigningKey ≥ 32.
                var signingKey = jwt.SigningKey is { Length: >= 32 }
                    ? jwt.SigningKey
                    : new string('0', 32);

                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                // No enriquecer logs con el header Authorization.
                bearerOptions.IncludeErrorDetails = environment.IsDevelopment();
            });

    builder.Services.AddAuthorization();
    builder.Services.AddExceptionHandler<ApiProblemExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            // ModelState inválido → ApiProblem validation (E3/E5), no ValidationProblemDetails suelto.
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value is { Errors.Count: > 0 })
                    .ToDictionary(
                        entry => ToCamelCaseKey(entry.Key),
                        entry => entry.Value!.Errors
                            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                                ? MessageCatalog.ValidationFailed
                                : e.ErrorMessage)
                            .ToArray(),
                        StringComparer.Ordinal);

                if (errors.Count == 0)
                {
                    errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
                    {
                        [string.Empty] = [MessageCatalog.ValidationFailed]
                    };
                }

                var problem = ApiProblemFactory.Validation(context.HttpContext, errors);
                return new ObjectResult(problem) { StatusCode = problem.Status };
            };
        });
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // R10: seeder admin solo Development (password vía user-secrets/env, nunca en repo).
    if (app.Environment.IsDevelopment())
    {
        await DevelopmentAdminSeeder.EnsureAdminAsync(app.Services);
        app.MapOpenApi();
    }

    app.UseSerilogRequestLogging(options =>
    {
        // Mensaje por defecto: método, path, status, elapsed — sin headers sensibles.
        options.GetLevel = (httpContext, _, exception) =>
            exception is null
            && httpContext.Response.StatusCode < 500
                ? Serilog.Events.LogEventLevel.Information
                : Serilog.Events.LogEventLevel.Error;
    });

    app.UseExceptionHandler();
    app.UseStatusCodePages();

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "La API terminó de forma inesperada");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static string ToCamelCaseKey(string key)
{
    if (string.IsNullOrEmpty(key))
    {
        return string.Empty;
    }

    // ModelState puede traer rutas "NewPassword" o "request.UserName".
    var parts = key.Split('.');
    for (var i = 0; i < parts.Length; i++)
    {
        var p = parts[i];
        if (p.Length == 0)
        {
            continue;
        }

        parts[i] = char.ToLowerInvariant(p[0]) + p[1..];
    }

    return string.Join('.', parts);
}
