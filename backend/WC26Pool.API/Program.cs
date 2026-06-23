using Microsoft.EntityFrameworkCore;
using WC26Pool.API.BackgroundServices;
using WC26Pool.API.Data;
using WC26Pool.API.Endpoints;
using WC26Pool.API.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<ScoringService>();
builder.Services.AddScoped<PredictionOrderService>();
builder.Services.AddScoped<PredictionVisibilityService>();
builder.Services.AddScoped<FootballApiService>();
builder.Services.AddHttpClient<FootballApiService>();

builder.Services.AddHostedService<FootballPollingService>();

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

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));

app.Run();
