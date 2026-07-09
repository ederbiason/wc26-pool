using Microsoft.EntityFrameworkCore;
using WC26Pool.API.BackgroundServices;
using WC26Pool.API.Data;
using WC26Pool.API.Endpoints;
using WC26Pool.API.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null)));

builder.Services.AddScoped<ScoringService>();
builder.Services.AddScoped<PickemScoringService>();
builder.Services.AddScoped<PredictionVisibilityService>();
builder.Services.AddScoped<FootballApiService>();
builder.Services.AddHttpClient<FootballApiService>();

builder.Services.AddSingleton<FootballPollingService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<FootballPollingService>());

builder.Services.AddHostedService<PickemBracketSeedService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapMatchEndpoints();
app.MapPredictionEndpoints();
app.MapRankingEndpoints();
app.MapAdminEndpoints();
app.MapParticipantEndpoints();
app.MapPickemEndpoints();

app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "healthy", database = "connected", timestamp = DateTimeOffset.UtcNow });
    }
    catch
    {
        return Results.Json(new { status = "unhealthy", database = "disconnected", timestamp = DateTimeOffset.UtcNow }, statusCode: 503);
    }
});

app.Run();
