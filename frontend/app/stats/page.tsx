"use client";

import useSWR from "swr";
import { api } from "@/lib/api";
import { AppHeader } from "@/components/AppHeader";
import { useIdentity } from "@/components/IdentityProvider";
import type { ParticipantStats, MatchHighlight } from "@/types";

const STAGE_LABELS: Record<string, string> = {
  GROUP_STAGE: "Grupos",
  LAST_32: "Oitavas",
  LAST_16: "Oitavas",
  QUARTER_FINALS: "Quartas",
  SEMI_FINALS: "Semis",
  THIRD_PLACE: "3° Lugar",
  FINAL: "Final",
};

function stageBadge(stage: string): string {
  return STAGE_LABELS[stage] ?? stage;
}

function HighlightCard({
  emoji,
  title,
  name,
  value,
}: {
  emoji: string;
  title: string;
  name: string;
  value: string;
}) {
  return (
    <div className="flex-none w-44 bg-brand-surface2 border border-[#1E4A32] rounded-2xl p-4 flex flex-col gap-2 shadow-md">
      <span className="text-2xl leading-none">{emoji}</span>
      <p className="text-[#86B59A] text-[10px] font-bold uppercase tracking-widest leading-tight">
        {title}
      </p>
      <p className="text-brand-gold font-display text-lg leading-none tracking-wide truncate">
        {name}
      </p>
      <p className="text-[#E8F5E9] text-xs font-medium leading-tight">{value}</p>
    </div>
  );
}

function HighlightCardSkeleton() {
  return (
    <div className="flex-none w-44 bg-brand-surface2 border border-[#1E4A32] rounded-2xl p-4 flex flex-col gap-2">
      <div className="w-8 h-8 rounded-lg bg-brand-surface animate-pulse" />
      <div className="h-2.5 w-20 rounded bg-brand-surface animate-pulse" />
      <div className="h-5 w-28 rounded bg-brand-surface animate-pulse" />
      <div className="h-3 w-24 rounded bg-brand-surface animate-pulse" />
    </div>
  );
}

function StatsTableSkeleton() {
  return (
    <div className="flex flex-col gap-2">
      {Array.from({ length: 6 }).map((_, i) => (
        <div key={i} className="h-12 rounded-xl bg-brand-surface2 animate-pulse" />
      ))}
    </div>
  );
}

function MatchHighlightRow({
  match,
  label,
}: {
  match: MatchHighlight;
  label?: string;
}) {
  return (
    <div className="flex flex-col gap-1.5 p-3 bg-brand-surface2 border border-[#1E4A32] rounded-xl">
      <div className="flex items-center justify-between gap-2">
        <span className="text-[10px] font-bold uppercase tracking-widest text-[#86B59A] bg-[#1E4A32] rounded px-1.5 py-0.5">
          {stageBadge(match.stage)}
        </span>
        {label && (
          <span className="text-[10px] font-semibold text-brand-gold">{label}</span>
        )}
      </div>
      <div className="flex items-center gap-2">
        <span className="text-[#E8F5E9] text-sm font-semibold truncate flex-1">
          {match.homeTeam}
        </span>
        <span className="text-brand-gold font-display text-base tabular-nums leading-none px-1">
          {match.homeScore ?? "?"} × {match.awayScore ?? "?"}
        </span>
        <span className="text-[#E8F5E9] text-sm font-semibold truncate flex-1 text-right">
          {match.awayTeam}
        </span>
      </div>
      <div className="flex items-center gap-3 text-xs text-[#86B59A]">
        <span>🎯 {match.exactScoreCount} placar{match.exactScoreCount !== 1 ? "es" : ""} exato{match.exactScoreCount !== 1 ? "s" : ""}</span>
        <span>✅ {match.correctResultCount} resultado{match.correctResultCount !== 1 ? "s" : ""}</span>
      </div>
    </div>
  );
}

function MatchHighlightSkeleton() {
  return (
    <div className="flex flex-col gap-1.5 p-3 bg-brand-surface2 border border-[#1E4A32] rounded-xl">
      <div className="h-4 w-16 rounded bg-brand-surface animate-pulse" />
      <div className="h-5 w-full rounded bg-brand-surface animate-pulse" />
      <div className="h-3 w-32 rounded bg-brand-surface animate-pulse" />
    </div>
  );
}

export default function StatsPage() {
  const { identity } = useIdentity();
  const { data: stats, isLoading } = useSWR("stats", api.getStats);

  const topMatches = stats?.matchHighlights.slice(0, 5) ?? [];
  const bottomMatches = stats
    ? [...stats.matchHighlights]
        .sort(
          (a, b) =>
            a.exactScoreCount - b.exactScoreCount ||
            a.correctResultCount - b.correctResultCount
        )
        .slice(0, 5)
    : [];

  return (
    <div className="flex flex-col flex-1 pb-20">
      <AppHeader />
      <main className="flex-1 px-4 py-4 flex flex-col gap-8">
        {/* Header */}
        <div>
          <h1 className="font-display text-brand-gold text-3xl tracking-wider leading-none">
            ESTATÍSTICAS
          </h1>
          <p className="text-[#86B59A] text-sm mt-0.5">Desempenho do bolão</p>
        </div>

        {/* Seção 1 — Destaques */}
        <div className="flex flex-col gap-3">
          <h2 className="text-brand-gold text-xs font-bold uppercase tracking-widest">
            Destaques
          </h2>
          <div className="flex gap-3 overflow-x-auto -mx-4 px-4 pb-2 scrollbar-hide">
            {isLoading ? (
              Array.from({ length: 4 }).map((_, i) => (
                <HighlightCardSkeleton key={i} />
              ))
            ) : (
              <>
                <HighlightCard
                  emoji="🎯"
                  title="Rei dos Placares"
                  name={stats?.highlights.mostExactScores.participantName ?? "—"}
                  value={`${stats?.highlights.mostExactScores.count ?? 0} placares exatos`}
                />
                <HighlightCard
                  emoji="✅"
                  title="Mais Consistente"
                  name={stats?.highlights.bestHitRate.participantName ?? "—"}
                  value={`${stats?.highlights.bestHitRate.rate?.toFixed(1) ?? 0}% de aproveitamento`}
                />
                <HighlightCard
                  emoji="⚡"
                  title="Rei do Mata-Mata"
                  name={stats?.highlights.bestKnockout.participantName ?? "—"}
                  value={`${stats?.highlights.bestKnockout.points ?? 0} pts eliminatórias`}
                />
                <HighlightCard
                  emoji="🏆"
                  title="Líder do Pick'em"
                  name={stats?.highlights.pickemLeader.participantName ?? "—"}
                  value={`${stats?.highlights.pickemLeader.points ?? 0} pts no pick'em`}
                />
              </>
            )}
          </div>
        </div>

        {/* Seção 2 — Tabela por participante */}
        <div className="flex flex-col gap-3">
          <h2 className="text-brand-gold text-xs font-bold uppercase tracking-widest">
            Por participante
          </h2>
          {isLoading ? (
            <StatsTableSkeleton />
          ) : (
            <div className="overflow-x-auto -mx-4 px-4">
              <table className="w-full min-w-[560px] text-xs border-separate border-spacing-y-1.5">
                <thead>
                  <tr>
                    <th className="text-left text-[#86B59A] font-bold uppercase tracking-widest pb-1 pr-3">
                      Participante
                    </th>
                    <th className="text-center text-[#86B59A] font-bold uppercase tracking-widest pb-1 px-2">
                      Pts
                    </th>
                    <th className="text-center text-[#86B59A] font-bold uppercase tracking-widest pb-1 px-2">
                      🎯
                    </th>
                    <th className="text-center text-[#86B59A] font-bold uppercase tracking-widest pb-1 px-2">
                      ✅
                    </th>
                    <th className="text-center text-[#86B59A] font-bold uppercase tracking-widest pb-1 px-2">
                      Aprov.
                    </th>
                    <th className="text-center text-[#86B59A] font-bold uppercase tracking-widest pb-1 px-2">
                      Grupos
                    </th>
                    <th className="text-center text-[#86B59A] font-bold uppercase tracking-widest pb-1 px-2">
                      Mata
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {stats?.participants
                    .slice()
                    .sort((a, b) => b.totalPoints - a.totalPoints)
                    .map((p: ParticipantStats) => {
                      const isMe = p.participantId === identity?.participantId;
                      return (
                        <tr
                          key={p.participantId}
                          className={`rounded-xl ${
                            isMe
                              ? "bg-brand-gold/10"
                              : "bg-brand-surface2"
                          }`}
                        >
                          <td
                            className={`py-2.5 pl-3 pr-2 rounded-l-xl font-semibold truncate max-w-[120px] ${
                              isMe ? "text-brand-gold" : "text-[#E8F5E9]"
                            }`}
                          >
                            {p.participantName}
                            {isMe && (
                              <span className="ml-1.5 text-[9px] font-bold uppercase tracking-widest text-brand-gold/70">
                                você
                              </span>
                            )}
                          </td>
                          <td className="py-2.5 px-2 text-center font-display text-base text-brand-gold tabular-nums">
                            {p.totalPoints}
                          </td>
                          <td className="py-2.5 px-2 text-center text-[#E8F5E9] tabular-nums">
                            {p.exactScores}
                          </td>
                          <td className="py-2.5 px-2 text-center text-[#E8F5E9] tabular-nums">
                            {p.correctResults}
                          </td>
                          <td className="py-2.5 px-2 text-center text-[#E8F5E9] tabular-nums">
                            {p.hitRate.toFixed(1)}%
                          </td>
                          <td className="py-2.5 px-2 text-center text-[#E8F5E9] tabular-nums">
                            {p.pointsByStage.groupStage}
                          </td>
                          <td className="py-2.5 pr-3 pl-2 text-center rounded-r-xl text-[#E8F5E9] tabular-nums">
                            {p.pointsByStage.knockoutStage}
                          </td>
                        </tr>
                      );
                    })}
                </tbody>
              </table>
              <div className="mt-2 flex gap-4 text-[10px] text-[#86B59A]">
                <span>🎯 Placares exatos</span>
                <span>✅ Resultados corretos</span>
                <span>Mata = Eliminatórias</span>
              </div>
            </div>
          )}
        </div>

        {/* Seção 3 — Jogos mais acertados */}
        <div className="flex flex-col gap-3">
          <h2 className="text-brand-gold text-xs font-bold uppercase tracking-widest">
            🎯 Jogos mais acertados
          </h2>
          {isLoading ? (
            <div className="flex flex-col gap-2">
              {Array.from({ length: 3 }).map((_, i) => (
                <MatchHighlightSkeleton key={i} />
              ))}
            </div>
          ) : topMatches.length === 0 ? (
            <p className="text-[#86B59A] text-sm italic">
              Nenhum jogo pontuado ainda.
            </p>
          ) : (
            <div className="flex flex-col gap-2">
              {topMatches.map((m, i) => (
                <MatchHighlightRow
                  key={m.matchId}
                  match={m}
                  label={i === 0 ? "🏅 Mais fácil do bolão" : undefined}
                />
              ))}
            </div>
          )}
        </div>

        {/* Seção 4 — Jogos mais errados */}
        <div className="flex flex-col gap-3">
          <h2 className="text-brand-gold text-xs font-bold uppercase tracking-widest">
            💀 Jogos mais errados
          </h2>
          {isLoading ? (
            <div className="flex flex-col gap-2">
              {Array.from({ length: 3 }).map((_, i) => (
                <MatchHighlightSkeleton key={i} />
              ))}
            </div>
          ) : bottomMatches.length === 0 ? (
            <p className="text-[#86B59A] text-sm italic">
              Nenhum jogo pontuado ainda.
            </p>
          ) : (
            <div className="flex flex-col gap-2">
              {bottomMatches.map((m, i) => (
                <MatchHighlightRow
                  key={m.matchId}
                  match={m}
                  label={i === 0 ? "💀 Mais difícil do bolão" : undefined}
                />
              ))}
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
