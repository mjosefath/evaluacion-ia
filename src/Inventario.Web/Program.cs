using Inventario.ApiClient;
using Inventario.ApiClient.Auth;
using Inventario.Web.Auth;
using Inventario.Web.Filters;
using Inventario.Web.Options;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

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
        options.AccessDeniedPath = "/Account/Login";
        options.ReturnUrlParameter = "returnUrl";
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<InventarioApiExceptionFilter>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<InventarioApiExceptionFilter>();
});

var app = builder.Build();

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

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
