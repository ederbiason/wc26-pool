"use client";

import useSWR from "swr";
import { api, formatTimeBrasilia, formatDateBrasilia } from "@/lib/api";
import type { Match, UpcomingDay } from "@/types";
import { MatchCardSkeleton } from "@/components/MatchCardSkeleton";

function CalendarMatchCard({ match }: { match: Match }) {
  return (
    <div className="bg-brand-surface rounded-xl border border-[#1E4A32] px-4 py-3 flex items-center gap-4">
      <div className="flex items-center justify-center w-10 flex-none">
        {match.homeTeamFlag ? (
          <img
            src={match.homeTeamFlag}
            alt={match.homeTeam}
            className="w-9 h-9 rounded-lg object-cover"
          />
        ) : (
          <span className="text-xl">🏳️</span>
        )}
      </div>

      <div className="flex-1 flex flex-col gap-0.5 min-w-0">
        <span className="text-[#E8F5E9] text-sm font-semibold truncate">
          {match.homeTeam} × {match.awayTeam}
        </span>
        <span className="text-[#86B59A] text-xs">
          {formatTimeBrasilia(match.matchDate)} · Brasília
        </span>
      </div>

      <div className="flex items-center justify-center w-10 flex-none">
        {match.awayTeamFlag ? (
          <img
            src={match.awayTeamFlag}
            alt={match.awayTeam}
            className="w-9 h-9 rounded-lg object-cover"
          />
        ) : (
          <span className="text-xl">🏳️</span>
        )}
      </div>
    </div>
  );
}

export default function CalendarPage() {
  const { data: days, isLoading, error } = useSWR(
    "matches-upcoming",
    api.getMatchesUpcoming,
    { revalidateOnFocus: true }
  );

  const isEmpty = !isLoading && !error && (!days || days.length === 0);

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

        {isEmpty && (
          <div className="flex flex-col items-center justify-center py-16 gap-3">
            <span className="text-5xl">📅</span>
            <p className="text-[#86B59A] text-sm text-center">
              Nenhum jogo nos próximos dias.
            </p>
          </div>
        )}

        {days?.map((day: UpcomingDay) => (
          <section key={day.date} className="flex flex-col gap-2">
            <h2 className="text-brand-gold text-xs font-bold uppercase tracking-widest px-1 first-letter:uppercase">
              {formatDateBrasilia(`${day.date}T12:00:00`)}
            </h2>
            {day.matches.map((m) => (
              <CalendarMatchCard key={m.id} match={m} />
            ))}
          </section>
        ))}
      </main>
    </div>
  );
}
