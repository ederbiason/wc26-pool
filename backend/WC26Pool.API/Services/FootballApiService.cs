using System.Text.Json;
using WC26Pool.API.Models;

namespace WC26Pool.API.Services;

public class FootballApiService(HttpClient httpClient, IConfiguration configuration, ILogger<FootballApiService> logger)
{
    private readonly string _apiKey = configuration["FootballApi:ApiKey"] ?? string.Empty;
    private readonly string _baseUrl = configuration["FootballApi:BaseUrl"] ?? "https://api-football-v1.p.rapidapi.com/v3";
    private readonly int _leagueId = int.Parse(configuration["FootballApi:LeagueId"] ?? "1");

    public async Task<List<FootballApiMatch>> GetMatchesForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_baseUrl}/fixtures?league={_leagueId}&season=2026&date={date:yyyy-MM-dd}");

            request.Headers.Add("X-RapidAPI-Key", _apiKey);
            request.Headers.Add("X-RapidAPI-Host", "api-football-v1.p.rapidapi.com");

            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<FootballApiResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Response ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch matches for date {Date}", date);
            return [];
        }
    }
}

public record FootballApiResponse(List<FootballApiMatch> Response);

public record FootballApiMatch(
    FootballApiFixture Fixture,
    FootballApiTeams Teams,
    FootballApiGoals Goals
);

public record FootballApiFixture(
    int Id,
    FootballApiStatus Status,
    string Date
);

public record FootballApiStatus(string Short, string Long);

public record FootballApiTeams(FootballApiTeam Home, FootballApiTeam Away);

public record FootballApiTeam(int Id, string Name, string Logo);

public record FootballApiGoals(int? Home, int? Away);

public static class FootballApiMatchStatusMapper
{
    public static MatchStatus MapStatus(string shortStatus) => shortStatus switch
    {
        "NS" => MatchStatus.NotStarted,
        "1H" or "HT" or "2H" or "ET" or "BT" or "P" or "SUSP" or "INT" or "LIVE" => MatchStatus.InProgress,
        "FT" or "AET" or "PEN" => MatchStatus.Finished,
        _ => MatchStatus.NotStarted
    };
}
