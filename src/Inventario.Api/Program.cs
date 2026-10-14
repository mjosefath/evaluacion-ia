using System.Text;
using Inventario.Application;
using Inventario.Application.Options;
using Inventario.Data.Identity;
using Inventario.Data.System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
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
        })
        .AddEntityFrameworkStores<InventarioIdentityDbContext>()
        .AddDefaultTokenProviders();

    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
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
    builder.Services.AddProblemDetails();
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        // Mensaje por defecto: método, path, status, elapsed — sin headers sensibles.
        options.GetLevel = (httpContext, _, exception) =>
            exception is null
            && httpContext.Response.StatusCode < 500
                ? Serilog.Events.LogEventLevel.Information
                : Serilog.Events.LogEventLevel.Error;
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

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
