"use client";

import { useCallback } from "react";
import useSWR from "swr";
import { AppHeader } from "@/components/AppHeader";
import { MatchCard } from "@/components/MatchCard";
import { MatchCardSkeleton } from "@/components/MatchCardSkeleton";
import { api, todayDateParam } from "@/lib/api";
import type { Match, MatchWithVisibility } from "@/types";

const POLL_INTERVAL = 60_000;

function shouldPoll(matches: Match[] | undefined): boolean {
  if (!matches || matches.length === 0) return false;
  return matches.some(
    (m) => m.status === "NotStarted" || m.status === "InProgress"
  );
}

export default function HomePage() {
  const today = todayDateParam();

  const {
    data: matches,
    isLoading: matchesLoading,
    error: matchesError,
    mutate: mutateMatches,
  } = useSWR("matches-today", api.getMatchesToday, {
    refreshInterval: (data) => (shouldPoll(data) ? POLL_INTERVAL : 0),
  });

  const {
    data: matchesWithVisibility,
    mutate: mutateVisibility,
  } = useSWR(
    `predictions-day-${today}`,
    () => api.getPredictionsForDay(today),
    { refreshInterval: (data) => (shouldPollVisibility(data) ? POLL_INTERVAL : 0) }
  );

  const handlePredicted = useCallback(() => {
    mutateVisibility();
    mutateMatches();
  }, [mutateVisibility, mutateMatches]);

  const visibilityByMatchId = new Map<number, MatchWithVisibility["predictionVisibility"]>(
    matchesWithVisibility?.map((m) => [m.id, m.predictionVisibility]) ?? []
  );

  return (
    <div className="flex flex-col flex-1">
      <AppHeader />
      <main className="flex-1 px-4 py-4 flex flex-col gap-4">
        <div>
          <h1 className="font-display text-brand-gold text-3xl tracking-wider leading-none">
            JOGOS DE HOJE
          </h1>
          <p className="text-[#86B59A] text-sm mt-0.5">
            {new Date().toLocaleDateString("pt-BR", {
              timeZone: "America/Sao_Paulo",
              weekday: "long",
              day: "numeric",
              month: "long",
            })}
          </p>
        </div>

        {matchesLoading && (
          <div className="flex flex-col gap-4">
            {Array.from({ length: 3 }).map((_, i) => (
              <MatchCardSkeleton key={i} />
            ))}
          </div>
        )}

        {matchesError && (
          <div className="bg-red-500/10 border border-red-500/30 rounded-xl px-4 py-3">
            <p className="text-red-400 text-sm">
              Não foi possível carregar os jogos. Tente novamente.
            </p>
          </div>
        )}

        {!matchesLoading && !matchesError && matches?.length === 0 && (
          <div className="flex flex-col items-center justify-center py-16 gap-3">
            <span className="text-5xl">⚽</span>
            <p className="text-[#86B59A] text-sm text-center">
              Nenhum jogo hoje.
              <br />
              Confira a agenda para os próximos jogos.
            </p>
          </div>
        )}

        {!matchesLoading &&
          !matchesError &&
          matches?.map((match) => (
            <MatchCard
              key={match.id}
              match={match}
              visibility={visibilityByMatchId.get(match.id) ?? null}
              onPredicted={handlePredicted}
            />
          ))}
      </main>
    </div>
  );
}

function shouldPollVisibility(
  data: MatchWithVisibility[] | undefined
): boolean {
  if (!data || data.length === 0) return false;
  return data.some((m) => !m.predictionVisibility.isRevealed);
}
