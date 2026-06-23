"use client";

import type { RankingEntry } from "@/types";
import { useIdentity } from "@/components/IdentityProvider";

interface Props {
  ranking: RankingEntry[];
}

const MEDAL: Record<number, string> = { 1: "🥇", 2: "🥈", 3: "🥉" };

export function RankingTable({ ranking }: Props) {
  const { identity } = useIdentity();

  return (
    <div className="flex flex-col gap-2">
      {ranking.map((entry) => {
        const isMe = entry.participantId === identity?.participantId;
        return (
          <div
            key={entry.participantId}
            className={`flex items-center gap-4 px-4 py-3 rounded-xl border transition-all ${
              isMe ? "bg-brand-gold/10 border-brand-gold/40" : "bg-brand-surface border-[#1E4A32]"
            }`}
          >
            <span className="text-xl w-8 text-center flex-none">
              {MEDAL[entry.position] ?? (
                <span className="text-[#86B59A] text-sm font-bold tabular-nums">
                  {entry.position}
                </span>
              )}
            </span>

            <span
              className={`flex-1 font-semibold text-sm ${
                isMe ? "text-brand-gold" : "text-[#E8F5E9]"
              }`}
            >
              {entry.name}
              {isMe && (
                <span className="ml-2 text-[10px] font-bold uppercase tracking-widest text-brand-gold/70">
                  você
                </span>
              )}
            </span>

            <div className="flex flex-col items-end flex-none">
              <span
                className={`font-display text-2xl tabular-nums leading-none ${
                  isMe ? "text-brand-gold" : "text-[#E8F5E9]"
                }`}
              >
                {entry.totalPoints}
              </span>
              <span className="text-[#86B59A] text-[10px] uppercase tracking-widest">
                pts
              </span>
            </div>
          </div>
        );
      })}
    </div>
  );
}
