using Inventario.ApiClient;
using Inventario.ApiClient.Auth;
using Inventario.Web.Auth;
using Inventario.Web.Filters;
using Inventario.Web.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
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

        if (context.HostingEnvironment.IsDevelopment())
        {
            configuration.WriteTo.File(
                path: "logs/inventario-web-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14);
        }
    });

    builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));
    builder.Services.Configure<AuthCookieOptions>(builder.Configuration.GetSection(AuthCookieOptions.SectionName));

    var apiBaseUrl = builder.Configuration[$"{ApiOptions.SectionName}:BaseUrl"] ?? "https://localhost:7196/";
    var authOptions = builder.Configuration.GetSection(AuthCookieOptions.SectionName).Get<AuthCookieOptions>()
                      ?? new AuthCookieOptions();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IAuthTokenStore, CookieAuthTokenStore>();
    builder.Services.AddScoped<AuthSessionService>();
    builder.Services.AddInventarioApiClient(apiBaseUrl);

    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = authOptions.CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromDays(authOptions.CookieExpireDays);
            options.SlidingExpiration = true;
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ReturnUrlParameter = "returnUrl";
        });

    builder.Services.AddAuthorization();
    builder.Services.AddScoped<InventarioApiExceptionFilter>();
    builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.AddService<InventarioApiExceptionFilter>();
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        // Método, path, status, elapsed — sin Authorization ni cookies (L4).
        options.GetLevel = (httpContext, _, exception) =>
            exception is null
            && httpContext.Response.StatusCode < 500
                ? Serilog.Events.LogEventLevel.Information
                : Serilog.Events.LogEventLevel.Error;
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllers();
    app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "La Web terminó de forma inesperada");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
