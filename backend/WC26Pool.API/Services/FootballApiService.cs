using System.Text.Json;
using System.Text.Json.Serialization;
using WC26Pool.API.Models;

namespace WC26Pool.API.Services;

public class FootballApiService(HttpClient httpClient, IConfiguration configuration, ILogger<FootballApiService> logger)
{
    private readonly string _apiKey = configuration["FootballApi:ApiKey"] ?? string.Empty;
    private readonly string _baseUrl = configuration["FootballApi:BaseUrl"] ?? "https://api.football-data.org/v4";
    private readonly string _competitionId = configuration["FootballApi:LeagueId"] ?? "WC";

    public async Task<List<FootballApiMatch>> GetMatchesForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        try
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_baseUrl}/competitions/{_competitionId}/matches?dateFrom={dateStr}&dateTo={dateStr}");

            request.Headers.Add("X-Auth-Token", _apiKey);

            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<FootballApiResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Matches ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch matches for date {Date}", date);
            return [];
        }
    }
}

public record FootballApiResponse(List<FootballApiMatch> Matches);

public record FootballApiMatch(
    int Id,
    string UtcDate,
    string Status,
    FootballApiTeam HomeTeam,
    FootballApiTeam AwayTeam,
    FootballApiScore Score
);

public record FootballApiTeam(
    int Id,
    string Name,
    string Crest
);

public record FootballApiScore(
    FootballApiScoreDetail? FullTime
);

public record FootballApiScoreDetail(
    int? Home,
    int? Away
);

public static class FootballApiMatchStatusMapper
{
    public static MatchStatus MapStatus(string status) => status?.ToUpper() switch
    {
        "SCHEDULED" or "TIMED" or "POSTPONED" or "CANCELLED" or "SUSPENDED" => MatchStatus.NotStarted,
        "IN_PLAY" or "PAUSED" => MatchStatus.InProgress,
        "FINISHED" or "AWARDED" => MatchStatus.Finished,
        _ => MatchStatus.NotStarted
    };
}
