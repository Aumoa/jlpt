using JLPTWordbook.Components;
using JLPTWordbook.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddLocalization(o => o.ResourcesPath = "Localizations");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    string[] supportedCultures = ["en", "ko"];
    options.SetDefaultCulture("en")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(p => p.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Audience = "jlptwordbook";
    });
builder.Services.AddAuthorizationCore();

builder.Services.AddHttpClient();

builder.Services.Configure<OAuth2Options>(builder.Configuration.GetRequiredSection("OAuth2"));
builder.Services.AddSingleton<Worksheet>();
builder.Services.AddHostedService<Worksheet.BackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseRequestLocalization();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
