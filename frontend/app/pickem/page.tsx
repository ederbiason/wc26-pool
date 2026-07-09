"use client";

import useSWR from "swr";
import { useState } from "react";
import { toast } from "sonner";
import { AppHeader } from "@/components/AppHeader";
import { PickemBracketUI } from "@/components/PickemBracketUI";
import { useIdentity } from "@/components/IdentityProvider";
import { api } from "@/lib/api";
import type { PickemPickSubmission } from "@/types";

export default function PickemPage() {
  const { identity } = useIdentity();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { data: status, isLoading: statusLoading } = useSWR(
    "pickem-status",
    api.getPickemStatus
  );
  
  const { data: bracket, isLoading: bracketLoading } = useSWR(
    "pickem-bracket",
    api.getPickemBracket
  );

  const hasSubmitted = status?.completed.some(
    (p) => p.participantId === identity?.participantId
  );
  
  const { data: entry, mutate: mutateEntry, isLoading: entryLoading } = useSWR(
    hasSubmitted && identity ? `pickem-entry-${identity.participantId}` : null,
    () => api.getPickemEntry(identity!.participantId)
  );

  const isDeadlinePassed = status?.isRevealed;

  const handleSubmit = async (picks: PickemPickSubmission[]) => {
    if (!identity) return;
    setIsSubmitting(true);
    try {
      await api.submitPickemEntry({
        participantId: identity.participantId,
        picks,
      });
      toast.success("Pick'em salvo com sucesso!");
      // Atualizar dados localmente após submeter
      // Force reload to state 2
      window.location.reload();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Erro ao enviar pick'em.");
      setIsSubmitting(false);
    }
  };

  const isLoading = statusLoading || bracketLoading || (hasSubmitted && entryLoading);

  return (
    <div className="flex flex-col flex-1 pb-20">
      <AppHeader />
      <main className="flex-1 px-4 py-4 flex flex-col gap-6">
        <div>
          <h1 className="font-display text-brand-gold text-3xl tracking-wider leading-none">
            PICK'EM
          </h1>
          <p className="text-[#86B59A] text-sm mt-0.5">
            Mata-mata da Copa
          </p>
        </div>

        {/* Indicador de status */}
        {!isLoading && status && (
          <div className="flex flex-col gap-2 p-4 bg-brand-surface2 rounded-2xl border border-[#1E4A32]">
            <h3 className="text-brand-gold text-xs font-bold uppercase tracking-widest mb-1">
              Participantes
            </h3>
            <div className="grid grid-cols-2 gap-x-3 gap-y-1">
              {status.completed.map((p) => (
                <div key={p.participantId} className="flex items-center gap-2 text-xs">
                  <span className="text-base leading-none">✅</span>
                  <span className="text-[#E8F5E9] truncate">{p.participantName}</span>
                </div>
              ))}
              {status.pending.map((p) => (
                <div key={p.participantId} className="flex items-center gap-2 text-xs">
                  <span className="text-base leading-none">⏳</span>
                  <span className="text-[#86B59A] truncate">{p.participantName}</span>
                </div>
              ))}
            </div>
          </div>
        )}

        {isLoading ? (
          <div className="flex justify-center py-12 text-[#86B59A]">
            Carregando bracket...
          </div>
        ) : (
          <>
            {!hasSubmitted && isDeadlinePassed ? (
              <div className="flex flex-col items-center justify-center py-16 gap-3">
                <span className="text-5xl">🔒</span>
                <p className="text-[#86B59A] text-sm text-center">
                  Prazo encerrado.
                  <br />
                  Você não enviou seu bracket a tempo.
                </p>
              </div>
            ) : (
              bracket && (
                <PickemBracketUI
                  bracket={bracket}
                  entry={entry}
                  onSubmit={handleSubmit}
                  isSubmitting={isSubmitting}
                />
              )
            )}
          </>
        )}
      </main>
    </div>
  );
}
