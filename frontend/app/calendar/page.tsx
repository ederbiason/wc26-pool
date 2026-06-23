"use client";

import useSWR from "swr";
import { api, formatTimeBrasilia, formatDateBrasilia } from "@/lib/api";
import type { Match } from "@/types";
import { MatchCardSkeleton } from "@/components/MatchCardSkeleton";

function groupByDate(matches: Match[]): Record<string, Match[]> {
  return matches.reduce<Record<string, Match[]>>((acc, m) => {
    const key = new Date(m.matchDate).toLocaleDateString("sv-SE", {
      timeZone: "America/Sao_Paulo",
    });
    if (!acc[key]) acc[key] = [];
    acc[key].push(m);
    return acc;
  }, {});
}

function CalendarMatchCard({ match }: { match: Match }) {
  return (
    <div className="bg-brand-surface rounded-xl border border-[#1E4A32] px-4 py-3 flex items-center gap-4">
      <div className="flex flex-col items-center w-12 flex-none">
        {match.homeTeamFlag ? (
          <img
            src={match.homeTeamFlag}
            alt={match.homeTeam}
            className="w-8 h-8 rounded-lg object-cover"
          />
        ) : (
          <span className="text-xl">🏳️</span>
        )}
      </div>

      <div className="flex-1 flex flex-col gap-0.5">
        <span className="text-[#E8F5E9] text-sm font-semibold">
          {match.homeTeam} × {match.awayTeam}
        </span>
        <span className="text-[#86B59A] text-xs">
          {formatTimeBrasilia(match.matchDate)} · Brasília
        </span>
      </div>

      <div className="flex flex-col items-center w-12 flex-none">
        {match.awayTeamFlag ? (
          <img
            src={match.awayTeamFlag}
            alt={match.awayTeam}
            className="w-8 h-8 rounded-lg object-cover"
          />
        ) : (
          <span className="text-xl">🏳️</span>
        )}
      </div>
    </div>
  );
}

export default function CalendarPage() {
  const { data: matches, isLoading, error } = useSWR(
    "matches-upcoming",
    api.getMatchesUpcoming
  );

  const grouped = matches ? groupByDate(matches) : {};
  const dates = Object.keys(grouped).sort();

  return (
    <div className="flex flex-col flex-1">
      <header className="sticky top-0 z-30 bg-brand-green/95 backdrop-blur-md border-b border-[#1E4A32] px-4 h-14 flex items-center">
        <h1 className="font-display text-brand-gold text-2xl tracking-wider leading-none">
          AGENDA
        </h1>
      </header>

      <main className="flex-1 px-4 py-4 flex flex-col gap-6">
        {isLoading && (
          <div className="flex flex-col gap-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <MatchCardSkeleton key={i} />
            ))}
          </div>
        )}

        {error && (
          <div className="bg-red-500/10 border border-red-500/30 rounded-xl px-4 py-3">
            <p className="text-red-400 text-sm">
              Não foi possível carregar a agenda.
            </p>
          </div>
        )}

        {!isLoading && !error && matches?.length === 0 && (
          <div className="flex flex-col items-center justify-center py-16 gap-3">
            <span className="text-5xl">📅</span>
            <p className="text-[#86B59A] text-sm text-center">
              Nenhum jogo nos próximos dias.
            </p>
          </div>
        )}

        {dates.map((date) => (
          <section key={date} className="flex flex-col gap-2">
            <h2 className="text-brand-gold text-xs font-bold uppercase tracking-widest px-1">
              {formatDateBrasilia(grouped[date][0].matchDate)}
            </h2>
            {grouped[date].map((m) => (
              <CalendarMatchCard key={m.id} match={m} />
            ))}
          </section>
        ))}
      </main>
    </div>
  );
}
