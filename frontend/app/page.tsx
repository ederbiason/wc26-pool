"use client";

import { useCallback, useState } from "react";
import useSWR from "swr";
import { AppHeader } from "@/components/AppHeader";
import { MatchCard } from "@/components/MatchCard";
import { MatchCardSkeleton } from "@/components/MatchCardSkeleton";
import { api, formatDateBrasilia } from "@/lib/api";
import type { MatchWithVisibility } from "@/types";

const POLL_INTERVAL = 60_000;

type DayView = "today" | "yesterday";

function shouldPoll(matches: MatchWithVisibility[] | undefined): boolean {
  if (!matches || matches.length === 0) return false;
  return matches.some(
    (m) => m.status === "NotStarted" || m.status === "InProgress"
  );
}

function yesterdayDateParam(): string {
  const d = new Date();
  d.setDate(d.getDate() - 1);
  return d.toLocaleDateString("sv-SE", { timeZone: "America/Sao_Paulo" });
}

function todayDateParam(): string {
  return new Date().toLocaleDateString("sv-SE", {
    timeZone: "America/Sao_Paulo",
  });
}

function formatDisplayDate(isoDate: string): string {
  return formatDateBrasilia(`${isoDate}T12:00:00`);
}

export default function HomePage() {
  const [view, setView] = useState<DayView>("today");

  const yesterdayDate = yesterdayDateParam();
  const todayDate = todayDateParam();

  const {
    data: todayMatches,
    isLoading: todayLoading,
    error: todayError,
    mutate: mutateToday,
  } = useSWR("matches-today", api.getMatchesToday, {
    refreshInterval: (data) => (shouldPoll(data) ? POLL_INTERVAL : 0),
  });

  const {
    data: yesterdayMatches,
    isLoading: yesterdayLoading,
    error: yesterdayError,
    mutate: mutateYesterday,
  } = useSWR(
    view === "yesterday" ? `matches-day-${yesterdayDate}` : null,
    () => api.getMatchesForDay(yesterdayDate)
  );

  const isToday = view === "today";
  const matches = isToday ? todayMatches : yesterdayMatches;
  const isLoading = isToday ? todayLoading : yesterdayLoading;
  const error = isToday ? todayError : yesterdayError;

  const handlePredicted = useCallback(() => {
    if (isToday) mutateToday();
    else mutateYesterday();
  }, [isToday, mutateToday, mutateYesterday]);

  const displayTitle = isToday ? "JOGOS DE HOJE" : "JOGOS DE ONTEM";
  const displayDate = isToday
    ? formatDisplayDate(todayDate)
    : formatDisplayDate(yesterdayDate);

  return (
    <div className="flex flex-col flex-1">
      <AppHeader />
      <main className="flex-1 px-4 py-4 flex flex-col gap-4">
        <div>
          <h1 className="font-display text-brand-gold text-3xl tracking-wider leading-none">
            {displayTitle}
          </h1>
          <p className="text-[#86B59A] text-sm mt-0.5 capitalize">
            {displayDate}
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            id="btn-yesterday"
            onClick={() => setView("yesterday")}
            className={`flex-1 flex items-center justify-center gap-1.5 h-9 rounded-xl text-xs font-bold uppercase tracking-widest transition-all ${
              view === "yesterday"
                ? "bg-brand-gold text-brand-green"
                : "bg-brand-surface border border-[#1E4A32] text-[#86B59A] hover:border-brand-gold/50 hover:text-[#E8F5E9]"
            }`}
          >
            <span>←</span>
            Ontem
          </button>
          <button
            id="btn-today"
            onClick={() => setView("today")}
            className={`flex-1 flex items-center justify-center gap-1.5 h-9 rounded-xl text-xs font-bold uppercase tracking-widest transition-all ${
              view === "today"
                ? "bg-brand-gold text-brand-green"
                : "bg-brand-surface border border-[#1E4A32] text-[#86B59A] hover:border-brand-gold/50 hover:text-[#E8F5E9]"
            }`}
          >
            Hoje
            <span>→</span>
          </button>
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
              {isToday ? (
                <>
                  Nenhum jogo hoje.
                  <br />
                  Confira a agenda para os próximos jogos.
                </>
              ) : (
                "Nenhum jogo ontem."
              )}
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
