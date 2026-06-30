using WC26Pool.API.BackgroundServices;
using WC26Pool.API.Services;

namespace WC26Pool.API.Tests;

public class ResolveMatchScoreTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Caso 1 — Jogo decidido no tempo normal (REGULAR)
    // regularTime não existe no JSON da API nesse caso
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Regular_ReturnsFullTime()
    {
        var score = new FootballApiScore(
            Winner:      "HOME_TEAM",
            Duration:    "REGULAR",
            FullTime:    new FootballApiScoreDetail(2, 1),
            RegularTime: null,
            ExtraTime:   null,
            Penalties:   null
        );

        var (home, away) = FootballPollingService.ResolveMatchScore(score);

        Assert.Equal(2, home);
        Assert.Equal(1, away);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Caso 2 — Jogo decidido nos pênaltis (Germany x Paraguay)
    // fullTime = 4x5, mas HomeScore/AwayScore deve ser 1x1
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public void PenaltyShootout_ReturnsRegularPlusExtraTime_NotFullTime()
    {
        var score = new FootballApiScore(
            Winner:      "AWAY_TEAM",
            Duration:    "PENALTY_SHOOTOUT",
            FullTime:    new FootballApiScoreDetail(4, 5),
            RegularTime: new FootballApiScoreDetail(1, 1),
            ExtraTime:   new FootballApiScoreDetail(0, 0),
            Penalties:   new FootballApiScoreDetail(3, 4)
        );

        var (home, away) = FootballPollingService.ResolveMatchScore(score);

        Assert.Equal(1, home);
        Assert.Equal(1, away);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Caso 3 — Jogo decidido na prorrogação sem pênaltis
    // HomeScore/AwayScore deve ser regularTime + extraTime
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public void ExtraTime_ReturnsRegularPlusExtraTime()
    {
        var score = new FootballApiScore(
            Winner:      "HOME_TEAM",
            Duration:    "EXTRA_TIME",
            FullTime:    new FootballApiScoreDetail(2, 1),
            RegularTime: new FootballApiScoreDetail(1, 1),
            ExtraTime:   new FootballApiScoreDetail(1, 0),
            Penalties:   null
        );

        var (home, away) = FootballPollingService.ResolveMatchScore(score);

        Assert.Equal(2, home);
        Assert.Equal(1, away);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Caso 4 — Segundo caso real de pênaltis (Netherlands x Morocco)
    // fullTime = 3x4, mas HomeScore/AwayScore deve ser 1x1
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public void PenaltyShootout_SecondRealCase_ReturnsRegularPlusExtraTime()
    {
        var score = new FootballApiScore(
            Winner:      "AWAY_TEAM",
            Duration:    "PENALTY_SHOOTOUT",
            FullTime:    new FootballApiScoreDetail(3, 4),
            RegularTime: new FootballApiScoreDetail(1, 1),
            ExtraTime:   new FootballApiScoreDetail(0, 0),
            Penalties:   new FootballApiScoreDetail(2, 3)
        );

        var (home, away) = FootballPollingService.ResolveMatchScore(score);

        Assert.Equal(1, home);
        Assert.Equal(1, away);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Caso extra — score null (jogo ainda não iniciado)
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public void NullScore_ReturnsBothNull()
    {
        var (home, away) = FootballPollingService.ResolveMatchScore(null);

        Assert.Null(home);
        Assert.Null(away);
    }
}
