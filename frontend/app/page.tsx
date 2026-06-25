"use client";

import { useCallback } from "react";
import useSWR from "swr";
import { AppHeader } from "@/components/AppHeader";
import { MatchCard } from "@/components/MatchCard";
import { MatchCardSkeleton } from "@/components/MatchCardSkeleton";
import { api } from "@/lib/api";
import type { MatchWithVisibility } from "@/types";

const POLL_INTERVAL = 60_000;

function shouldPoll(matches: MatchWithVisibility[] | undefined): boolean {
  if (!matches || matches.length === 0) return false;
  return matches.some(
    (m) => m.status === "NotStarted" || m.status === "InProgress"
  );
}

export default function HomePage() {
  const {
    data: matches,
    isLoading,
    error,
    mutate,
  } = useSWR("matches-today", api.getMatchesToday, {
    refreshInterval: (data) => (shouldPoll(data) ? POLL_INTERVAL : 0),
  });

  const handlePredicted = useCallback(() => {
    mutate();
  }, [mutate]);

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

        {isLoading && (
          <div className="flex flex-col gap-4">
            {Array.from({ length: 3 }).map((_, i) => (
              <MatchCardSkeleton key={i} />
            ))}
          </div>
        )}

        {error && (
          <div className="bg-red-500/10 border border-red-500/30 rounded-xl px-4 py-3">
            <p className="text-red-400 text-sm">
              Não foi possível carregar os jogos. Tente novamente.
            </p>
          </div>
        )}

        {!isLoading && !error && matches?.length === 0 && (
          <div className="flex flex-col items-center justify-center py-16 gap-3">
            <span className="text-5xl">⚽</span>
            <p className="text-[#86B59A] text-sm text-center">
              Nenhum jogo hoje.
              <br />
              Confira a agenda para os próximos jogos.
            </p>
          </div>
        )}

        {!isLoading &&
          !error &&
          matches?.map((match) => (
            <MatchCard
              key={match.id}
              match={match}
              visibility={match.predictionVisibility}
              onPredicted={handlePredicted}
            />
          ))}
      </main>
    </div>
  );
}
