using WC26Pool.API.Services;

namespace WC26Pool.API.Tests;

/// <summary>
/// Testa a lógica de pontuação de jogos mata-mata cobrindo todos os cenários
/// definidos na tabela de pontuação oficial do grupo.
/// </summary>
public class KnockoutScoringTests
{
    // Parâmetros fixos: stage mata-mata, duration PENALTY_SHOOTOUT
    // Placar do jogo real: 1×1 + HOME ganha nos pênaltis
    private const string Knockout = "QUARTER_FINALS";
    private const string Regular  = "GROUP_STAGE";

    // ──────────────────────────────────────────────────────────────────────────
    // Caso 1 — Palpite 2×1, resultado 2×1 → 2 pontos (placar exato, REGULAR)
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Case1_ExactScore_Regular_Returns2()
    {
        var points = ScoringService.CalculatePoints(
            predHome: 2, predAway: 1, predPenaltyWinner: null,
            actualHome: 2, actualAway: 1,
            stage: Knockout, duration: "REGULAR",
            penaltyHome: null, penaltyAway: null);

        Assert.Equal(2, points);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Caso 2 — Palpite 1×0, resultado 2×1 → 1 ponto (acertou quem ganhou)
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Case2_CorrectWinner_WrongScore_Returns1()
    {
        var points = ScoringService.CalculatePoints(
            predHome: 1, predAway: 0, predPenaltyWinner: null,
            actualHome: 2, actualAway: 1,
            stage: Knockout, duration: "REGULAR",
            penaltyHome: null, penaltyAway: null);

        Assert.Equal(1, points);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Caso 3 — Palpite 1×1 + HOME, resultado 1×1 + HOME pens → 3 pontos
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Case3_ExactScoreAndPenaltyWinner_Returns3()
    {
        var points = ScoringService.CalculatePoints(
            predHome: 1, predAway: 1, predPenaltyWinner: "HOME",
            actualHome: 1, actualAway: 1,
            stage: Knockout, duration: "PENALTY_SHOOTOUT",
            penaltyHome: 4, penaltyAway: 2);

        Assert.Equal(3, points);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Caso 4 — Palpite 1×1 + HOME, resultado 1×1 + AWAY pens → 2 pontos
    // (placar exato correto, pênaltis errado)
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Case4_ExactScoreWrongPenaltyWinner_Returns2()
    {
        var points = ScoringService.CalculatePoints(
            predHome: 1, predAway: 1, predPenaltyWinner: "HOME",
            actualHome: 1, actualAway: 1,
            stage: Knockout, duration: "PENALTY_SHOOTOUT",
            penaltyHome: 2, penaltyAway: 4);

        Assert.Equal(2, points);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Caso 5 — Palpite 1×1 + HOME, resultado 2×2 + HOME pens → 2 pontos (nova regra)
    // (placar exato errado, mas empate + vencedor dos pênaltis correto)
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Case5_DrawPrediction_CorrectPenaltyWinner_WrongExactScore_Returns2()
    {
        var points = ScoringService.CalculatePoints(
            predHome: 1, predAway: 1, predPenaltyWinner: "HOME",
            actualHome: 2, actualAway: 2,
            stage: Knockout, duration: "PENALTY_SHOOTOUT",
            penaltyHome: 4, penaltyAway: 2);

        Assert.Equal(2, points);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Caso 6 — Palpite 1×1 + HOME, resultado 2×2 + AWAY pens → 1 ponto
    // (empate correto, mas vencedor dos pênaltis errado)
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Case6_DrawPrediction_WrongPenaltyWinner_WrongExactScore_Returns1()
    {
        var points = ScoringService.CalculatePoints(
            predHome: 1, predAway: 1, predPenaltyWinner: "HOME",
            actualHome: 2, actualAway: 2,
            stage: Knockout, duration: "PENALTY_SHOOTOUT",
            penaltyHome: 2, penaltyAway: 4);

        Assert.Equal(1, points);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Caso 7 — Palpite 2×1, resultado 1×1 + HOME pens → 1 ponto
    // (acertou quem se classificou, mas via placar errado)
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Case7_PredictedHomeWin_ActualPenaltyHomeWin_Returns1()
    {
        var points = ScoringService.CalculatePoints(
            predHome: 2, predAway: 1, predPenaltyWinner: null,
            actualHome: 1, actualAway: 1,
            stage: Knockout, duration: "PENALTY_SHOOTOUT",
            penaltyHome: 4, penaltyAway: 2);

        Assert.Equal(1, points);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Caso 8 — Palpite 2×1, resultado 0×1 → 0 pontos (errou tudo)
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Case8_PredictedHomeWin_ActualAwayWin_Returns0()
    {
        var points = ScoringService.CalculatePoints(
            predHome: 2, predAway: 1, predPenaltyWinner: null,
            actualHome: 0, actualAway: 1,
            stage: Knockout, duration: "REGULAR",
            penaltyHome: null, penaltyAway: null);

        Assert.Equal(0, points);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Sanidade — Fase de grupos: placar exato → 2 pontos
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public void GroupStage_ExactScore_Returns2()
    {
        var points = ScoringService.CalculatePoints(
            predHome: 3, predAway: 1, predPenaltyWinner: null,
            actualHome: 3, actualAway: 1,
            stage: Regular, duration: "REGULAR",
            penaltyHome: null, penaltyAway: null);

        Assert.Equal(2, points);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Sanidade — Fase de grupos: resultado correto, placar errado → 1 ponto
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public void GroupStage_CorrectResult_WrongScore_Returns1()
    {
        var points = ScoringService.CalculatePoints(
            predHome: 1, predAway: 0, predPenaltyWinner: null,
            actualHome: 3, actualAway: 1,
            stage: Regular, duration: "REGULAR",
            penaltyHome: null, penaltyAway: null);

        Assert.Equal(1, points);
    }
}
