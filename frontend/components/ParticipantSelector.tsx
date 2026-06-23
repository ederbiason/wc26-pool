"use client";

import { useEffect, useState } from "react";
import type { Participant, StoredIdentity } from "@/types";
import { api } from "@/lib/api";

interface Props {
  onSelect: (identity: StoredIdentity) => void;
}

export function ParticipantSelector({ onSelect }: Props) {
  const [participants, setParticipants] = useState<Participant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .getParticipants()
      .then(setParticipants)
      .catch(() => setError("Não foi possível carregar os participantes."))
      .finally(() => setLoading(false));
  }, []);

  const handleSelect = (p: Participant) => {
    onSelect({
      participantId: p.id,
      participantName: p.name,
      isAdmin: p.isAdmin,
    });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/70 backdrop-blur-sm sm:items-center">
      <div className="w-full max-w-lg bg-brand-surface rounded-t-3xl sm:rounded-2xl p-6 pb-10 sm:pb-6 animate-in slide-in-from-bottom-8 duration-300">
        <div className="mb-1 text-center">
          <span className="font-display text-brand-gold text-4xl tracking-wider leading-none">
            BOLÃO DOS NOJEIRAS
          </span>
        </div>
        <p className="text-center text-[#86B59A] text-sm mb-7">
          Copa do Mundo 2026 · Quem é você?
        </p>

        {loading && (
          <div className="grid grid-cols-2 gap-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <div
                key={i}
                className="h-16 rounded-xl bg-brand-surface2 animate-pulse"
              />
            ))}
          </div>
        )}

        {error && (
          <p className="text-red-400 text-center text-sm">{error}</p>
        )}

        {!loading && !error && (
          <div className="grid grid-cols-2 gap-3">
            {participants.map((p) => (
              <button
                key={p.id}
                id={`participant-${p.id}`}
                onClick={() => handleSelect(p)}
                className="touch-target flex flex-col items-center justify-center gap-1 rounded-xl bg-brand-surface2 border border-[#1E4A32] hover:border-brand-gold hover:bg-[#223D2B] active:scale-95 transition-all duration-150 px-4 py-3 cursor-pointer"
              >
                <span className="text-2xl">⚽</span>
                <span className="font-semibold text-[#E8F5E9] text-sm leading-tight text-center">
                  {p.name}
                </span>
                {p.isAdmin && (
                  <span className="text-[10px] text-brand-gold font-medium uppercase tracking-widest">
                    Admin
                  </span>
                )}
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
