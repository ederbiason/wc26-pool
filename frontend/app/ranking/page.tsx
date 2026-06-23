"use client";

import useSWR from "swr";
import { api } from "@/lib/api";
import { RankingTable } from "@/components/RankingTable";

export default function RankingPage() {
  const { data: ranking, isLoading, error } = useSWR(
    "ranking",
    api.getRanking,
    { refreshInterval: 30_000 }
  );

  return (
    <div className="flex flex-col flex-1">
      <header className="sticky top-0 z-30 bg-brand-green/95 backdrop-blur-md border-b border-[#1E4A32] px-4 h-14 flex items-center">
        <h1 className="font-display text-brand-gold text-2xl tracking-wider leading-none">
          RANKING
        </h1>
      </header>

      <main className="flex-1 px-4 py-4 flex flex-col gap-4">
        {isLoading && (
          <div className="flex flex-col gap-2">
            {Array.from({ length: 6 }).map((_, i) => (
              <div
                key={i}
                className="h-16 rounded-xl bg-brand-surface border border-[#1E4A32] animate-pulse"
              />
            ))}
          </div>
        )}

        {error && (
          <div className="bg-red-500/10 border border-red-500/30 rounded-xl px-4 py-3">
            <p className="text-red-400 text-sm">
              Não foi possível carregar o ranking.
            </p>
          </div>
        )}

        {!isLoading && !error && ranking && (
          <RankingTable ranking={ranking} />
        )}
      </main>
    </div>
  );
}
