using System.Globalization;
using FraudDetection.Web.Data;
using FraudDetection.Web.Middleware;
using FraudDetection.Web.Services;
using FraudDetection.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- MVC ----------
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// ---------- Banco de dados (MySQL via Docker, porta 3307) ----------
string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não configurada.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ---------- Autenticação por cookie ----------
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// ---------- Consumo da API de análise de fraude ----------
// O motor de fraude agora é uma API externa. Registramos um HttpClient tipado
// que aponta para a URL configurada e implementa IFraudDetectionService.
string fraudApiBaseUrl =
    builder.Configuration["FraudApi:BaseUrl"]
    ?? throw new InvalidOperationException("'FraudApi:BaseUrl' não configurada.");

builder.Services.AddHttpClient<IFraudDetectionService, FraudApiClient>(client =>
{
    client.BaseAddress = new Uri(fraudApiBaseUrl);
});

// ---------- Serviços de domínio ----------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

// ---------- Cultura pt-BR (formato de moeda R$ e datas) ----------
CultureInfo[] supportedCultures = [new CultureInfo("pt-BR")];
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-BR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// ---------- Pipeline ----------
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
