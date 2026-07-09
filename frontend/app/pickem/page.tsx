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
  const [activeTabId, setActiveTabId] = useState<number | null>(null);

  const { data: status, isLoading: statusLoading } = useSWR(
    "pickem-status",
    api.getPickemStatus
  );
  
  const { data: bracket, isLoading: bracketLoading } = useSWR(
    "pickem-bracket",
    api.getPickemBracket
  );

  const allParticipants = status
    ? [...status.completed, ...status.pending].sort((a, b) => {
        if (a.participantId === identity?.participantId) return -1;
        if (b.participantId === identity?.participantId) return 1;
        return 0;
      })
    : [];

  const selectedParticipantId = activeTabId ?? identity?.participantId;
  const isMe = selectedParticipantId === identity?.participantId;
  const isSelectedCompleted = status?.completed.some(
    (p) => p.participantId === selectedParticipantId
  );
  
  const { data: entry, mutate: mutateEntry, isLoading: entryLoading } = useSWR(
    isSelectedCompleted && selectedParticipantId
      ? `pickem-entry-${selectedParticipantId}`
      : null,
    () => api.getPickemEntry(selectedParticipantId!)
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

  const isLoading = statusLoading || bracketLoading || (isSelectedCompleted && entryLoading);

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

        {/* Tabs de Participantes */}
        {!isLoading && status && (
          <div className="flex overflow-x-auto gap-2 pb-2 -mx-4 px-4 scrollbar-hide">
            {allParticipants.map((p) => {
              const isParticipantMe = p.participantId === identity?.participantId;
              const isCompleted = status.completed.some(
                (c) => c.participantId === p.participantId
              );
              const isLocked = !status.isRevealed && !isParticipantMe;
              const isSelected = selectedParticipantId === p.participantId;

              return (
                <button
                  key={p.participantId}
                  onClick={() => {
                    if (!isLocked) setActiveTabId(p.participantId);
                  }}
                  disabled={isLocked}
                  className={`flex-none px-4 py-2 rounded-xl text-xs font-bold tracking-widest whitespace-nowrap transition-colors ${
                    isSelected
                      ? "bg-brand-gold text-brand-green"
                      : isLocked
                      ? "bg-brand-surface border border-[#1E4A32] text-[#86B59A] opacity-50 cursor-not-allowed"
                      : "bg-brand-surface border border-[#1E4A32] text-[#E8F5E9] hover:border-brand-gold/50"
                  }`}
                >
                  {isLocked && <span className="mr-1.5">🔒</span>}
                  {!isCompleted && !isLocked && <span className="mr-1.5">⏳</span>}
                  {isCompleted && !isLocked && <span className="mr-1.5">✅</span>}
                  {p.participantName}
                </button>
              );
            })}
          </div>
        )}

        {isLoading ? (
          <div className="flex justify-center py-12 text-[#86B59A]">
            Carregando bracket...
          </div>
        ) : (
          <>
            {!isSelectedCompleted && isDeadlinePassed ? (
              <div className="flex flex-col items-center justify-center py-16 gap-3">
                <span className="text-5xl">🔒</span>
                <p className="text-[#86B59A] text-sm text-center">
                  Prazo encerrado.
                  <br />
                  {isMe
                    ? "Você não enviou seu bracket a tempo."
                    : "Este participante não enviou o bracket a tempo."}
                </p>
              </div>
            ) : (
              bracket && (!isMe || isSelectedCompleted || !isDeadlinePassed) && (
                <PickemBracketUI
                  bracket={bracket}
                  entry={entry}
                  onSubmit={isMe ? handleSubmit : undefined}
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
