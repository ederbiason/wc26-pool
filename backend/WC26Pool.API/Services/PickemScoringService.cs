using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Data;
using WC26Pool.API.Models;

namespace WC26Pool.API.Services;

public class PickemScoringService(AppDbContext db, ILogger<PickemScoringService> logger)
{
    private static readonly HashSet<string> KnockoutStages =
        ["QUARTER_FINALS", "SEMI_FINALS", "FINAL"];

    /// Called after a knockout match is confirmed Finished.
    public async Task CalculatePickemPointsForMatchAsync(int matchId)
    {
        var match = await db.Matches.FindAsync(matchId);
        if (match is null) return;
        if (!KnockoutStages.Contains(match.Stage)) return;
        if (match.HomeScore is null || match.AwayScore is null) return;

        // Determine which team advanced (winner of this match)
        var winner = DetermineWinner(match);
        if (winner is null)
        {
            logger.LogWarning("Could not determine winner for match {Id} ({Stage})", matchId, match.Stage);
            return;
        }

        var loser = winner == match.HomeTeam ? match.AwayTeam : match.HomeTeam;

        // Map stage → pickem round
        var pickemRound = match.Stage switch
        {
            "QUARTER_FINALS" => "QUARTER_FINAL",
            "SEMI_FINALS"    => "SEMI_FINAL",
            "FINAL"          => "FINAL",
            _                => null
        };

        if (pickemRound is null) return;

        // Points per round
        var pointsForCorrect = match.Stage switch
        {
            "QUARTER_FINALS" => 1,
            "SEMI_FINALS"    => 2,
            "FINAL"          => 5,
            _                => 0
        };

        // Map QF slot to pickem slot index for this round
        // QF slots 0/1 → SEMI slot 0, QF slots 2/3 → SEMI slot 1, etc.
        // For SEMI_FINALS the winning QF teams fill slots 0 and 1 in order.
        // The pick SlotIndex in pickem mirrors the bracket position:
        //   QF: 0-3, SF: 0-1, F: 0
        // We can't know SlotIndex from the match alone without bracket context,
        // so we score ALL picks for the round that match the winner/loser team names.

        var entries = await db.PickemEntries
            .Include(e => e.Picks)
            .Include(e => e.Participant)
            .ToListAsync();

        foreach (var entry in entries)
        {
            // Find picks for this round that reference either participant
            var picksForRound = entry.Picks
                .Where(p => p.Round == pickemRound)
                .ToList();

            foreach (var pick in picksForRound)
            {
                if (pick.IsCorrect.HasValue) continue; // already scored

                if (pick.ChosenTeam == winner)
                {
                    pick.IsCorrect = true;
                    pick.PointsEarned = pointsForCorrect;
                }
                else if (pick.ChosenTeam == loser)
                {
                    // The team they picked was eliminated in this match
                    pick.IsCorrect = false;
                    pick.PointsEarned = 0;
                }
                // else: pick references a team not in this match — untouched until that match resolves
            }
        }

        await db.SaveChangesAsync();

        // Recalculate PickemStanding totals and propagate to Participant.TotalPoints
        foreach (var entry in entries)
        {
            var totalPickemPoints = entry.Picks
                .Where(p => p.PointsEarned.HasValue)
                .Sum(p => p.PointsEarned!.Value);

            var standing = await db.PickemStandings
                .FirstOrDefaultAsync(s => s.ParticipantId == entry.ParticipantId);

            // Capture the previous total BEFORE overwriting it
            var previousPickemPoints = standing?.TotalPickemPoints ?? 0;
            var delta = totalPickemPoints - previousPickemPoints;

            if (standing is null)
            {
                db.PickemStandings.Add(new PickemStanding
                {
                    ParticipantId = entry.ParticipantId,
                    TotalPickemPoints = totalPickemPoints
                });
            }
            else
            {
                standing.TotalPickemPoints = totalPickemPoints;
            }

            // Propagate delta to Participant.TotalPoints
            if (delta > 0 && entry.Participant is not null)
            {
                entry.Participant.TotalPoints += delta;
                logger.LogInformation(
                    "Participant {Id} earned {Delta} pickem point(s) (total pickem: {Total}).",
                    entry.ParticipantId, delta, totalPickemPoints);
            }
        }

        await db.SaveChangesAsync();

        // Update PickemBracketSlots — mark eliminated team
        var eliminatedSlot = await db.PickemBracketSlots
            .FirstOrDefaultAsync(s => s.TeamName == loser);

        if (eliminatedSlot is not null)
        {
            eliminatedSlot.IsEliminated = true;
            eliminatedSlot.EliminatedBy = winner;
            await db.SaveChangesAsync();
        }

        logger.LogInformation(
            "Pickem scoring done for match {Id} ({Stage}): {Winner} advances, {Loser} eliminated.",
            matchId, match.Stage, winner, loser);
    }

    private static string? DetermineWinner(Match match)
    {
        if (match.HomeScore is null || match.AwayScore is null) return null;

        if (match.Duration is "REGULAR" or "EXTRA_TIME")
        {
            if (match.HomeScore > match.AwayScore) return match.HomeTeam;
            if (match.AwayScore > match.HomeScore) return match.AwayTeam;
        }
        else if (match.Duration == "PENALTY_SHOOTOUT")
        {
            if (match.PenaltyHomeScore > match.PenaltyAwayScore) return match.HomeTeam;
            if (match.PenaltyAwayScore > match.PenaltyHomeScore) return match.AwayTeam;
        }

        return null;
    }
}
