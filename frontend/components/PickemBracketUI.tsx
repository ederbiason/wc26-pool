"use client";

import { useState } from "react";
import type { BracketSlot, PickemBracket, PickemPickSubmission, PickemEntry } from "@/types";

interface Props {
  bracket?: PickemBracket;
  entry?: PickemEntry;
  onSubmit?: (picks: PickemPickSubmission[]) => void;
  isSubmitting?: boolean;
}

export function PickemBracketUI({ bracket, entry, onSubmit, isSubmitting }: Props) {
  // Se já tem entry, exibe modo read-only
  const isReadOnly = !!entry;

  // Estado interativo
  const [qfPicks, setQfPicks] = useState<(BracketSlot | null)[]>([null, null, null, null]);
  const [sfPicks, setSfPicks] = useState<(BracketSlot | null)[]>([null, null]);
  const [finalPick, setFinalPick] = useState<BracketSlot | null>(null);

  // Derivar times das quartas se tiver bracket
  const qfSlots = bracket?.quarterFinals ?? [];

  // Helper para buscar o pick do entry read-only
  const getEntryPick = (round: string, slotIndex: number) => {
    if (!entry) return null;
    const pick = entry.picks.find((p) => p.round === round && p.slotIndex === slotIndex);
    if (!pick) return null;
    return {
      teamName: pick.chosenTeam,
      teamFlag: pick.chosenTeamFlag,
      isCorrect: pick.isCorrect,
    };
  };

  const handleQfPick = (matchIndex: number, team: BracketSlot) => {
    if (isReadOnly) return;
    const newQf = [...qfPicks];
    newQf[matchIndex] = team;
    setQfPicks(newQf);

    // Se mudar a QF e o time estava na SF, remover da SF
    const sfIndex = Math.floor(matchIndex / 2);
    if (sfPicks[sfIndex] && sfPicks[sfIndex]?.teamName !== team.teamName) {
      const newSf = [...sfPicks];
      newSf[sfIndex] = null;
      setSfPicks(newSf);
      // E também remover da Final se estiver lá
      if (finalPick && finalPick.teamName !== team.teamName) {
        setFinalPick(null);
      }
    }
  };

  const handleSfPick = (matchIndex: number, team: BracketSlot | null) => {
    if (isReadOnly || !team) return;
    const newSf = [...sfPicks];
    newSf[matchIndex] = team;
    setSfPicks(newSf);

    if (finalPick && finalPick.teamName !== team.teamName) {
      setFinalPick(null);
    }
  };

  const handleFinalPick = (team: BracketSlot | null) => {
    if (isReadOnly || !team) return;
    setFinalPick(team);
  };

  const handleSubmit = () => {
    if (!onSubmit || isReadOnly) return;
    
    const picks: PickemPickSubmission[] = [];
    
    qfPicks.forEach((pick, i) => {
      if (pick) {
        picks.push({
          round: "QUARTER_FINAL",
          slotIndex: i,
          chosenTeam: pick.teamName,
          chosenTeamFlag: pick.teamFlag,
        });
      }
    });

    sfPicks.forEach((pick, i) => {
      if (pick) {
        picks.push({
          round: "SEMI_FINAL",
          slotIndex: i,
          chosenTeam: pick.teamName,
          chosenTeamFlag: pick.teamFlag,
        });
      }
    });

    if (finalPick) {
      picks.push({
        round: "FINAL",
        slotIndex: 0,
        chosenTeam: finalPick.teamName,
        chosenTeamFlag: finalPick.teamFlag,
      });
    }

    onSubmit(picks);
  };

  const allPicked = qfPicks.every(Boolean) && sfPicks.every(Boolean) && finalPick !== null;

  const renderTeamCard = (
    team: { teamName: string; teamFlag: string } | null,
    isSelected: boolean,
    onClick: () => void,
    disabled: boolean,
    isCorrect?: boolean | null
  ) => {
    return (
      <button
        onClick={onClick}
        disabled={disabled || !team || isReadOnly}
        className={`flex items-center gap-3 w-full p-2.5 rounded-xl border transition-all text-left ${
          !team
            ? "border-[#1E4A32] border-dashed bg-brand-surface/50 opacity-50"
            : isSelected
            ? "border-brand-gold bg-brand-gold/10"
            : "border-[#1E4A32] bg-brand-surface hover:border-brand-gold/50"
        }`}
      >
        <div className="w-8 h-8 rounded-full overflow-hidden bg-brand-surface2 flex items-center justify-center flex-none">
          {team?.teamFlag ? (
            <img src={team.teamFlag} alt={team.teamName} className="w-full h-full object-cover" />
          ) : (
            <span className="text-sm">🏳️</span>
          )}
        </div>
        <span className={`text-sm font-semibold truncate flex-1 ${isSelected ? "text-brand-gold" : "text-[#E8F5E9]"}`}>
          {team?.teamName || "A definir"}
        </span>
        {isReadOnly && isCorrect === true && <span className="text-sm">✅</span>}
        {isReadOnly && isCorrect === false && <span className="text-sm">❌</span>}
      </button>
    );
  };

  return (
    <div className="flex flex-col gap-8 pb-8">
      <div className="flex flex-col gap-4">
        <h2 className="text-brand-gold text-xs font-bold uppercase tracking-widest text-center">
          Quartas de Final
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {[0, 1, 2, 3].map((matchIndex) => {
            const t1 = qfSlots.find((s) => s.slotIndex === matchIndex * 2);
            const t2 = qfSlots.find((s) => s.slotIndex === matchIndex * 2 + 1);

            const pickedTeam = isReadOnly
              ? getEntryPick("QUARTER_FINAL", matchIndex)
              : qfPicks[matchIndex];

            return (
              <div key={`qf-${matchIndex}`} className="flex flex-col gap-2 p-3 bg-brand-surface2 rounded-2xl border border-[#1E4A32]">
                {renderTeamCard(
                  t1 || null,
                  pickedTeam?.teamName === t1?.teamName,
                  () => t1 && handleQfPick(matchIndex, t1),
                  !t1,
                  isReadOnly ? pickedTeam?.teamName === t1?.teamName ? (pickedTeam as any)?.isCorrect : null : null
                )}
                <div className="flex justify-center -my-1 relative z-10">
                  <span className="bg-[#1E4A32] text-[#86B59A] text-[10px] font-bold px-2 py-0.5 rounded-full uppercase">
                    VS
                  </span>
                </div>
                {renderTeamCard(
                  t2 || null,
                  pickedTeam?.teamName === t2?.teamName,
                  () => t2 && handleQfPick(matchIndex, t2),
                  !t2,
                  isReadOnly ? pickedTeam?.teamName === t2?.teamName ? (pickedTeam as any)?.isCorrect : null : null
                )}
              </div>
            );
          })}
        </div>
      </div>

      <div className="flex flex-col gap-4">
        <h2 className="text-brand-gold text-xs font-bold uppercase tracking-widest text-center">
          Semifinais
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {[0, 1].map((matchIndex) => {
            const t1 = isReadOnly ? getEntryPick("QUARTER_FINAL", matchIndex * 2) : qfPicks[matchIndex * 2];
            const t2 = isReadOnly ? getEntryPick("QUARTER_FINAL", matchIndex * 2 + 1) : qfPicks[matchIndex * 2 + 1];

            const pickedTeam = isReadOnly
              ? getEntryPick("SEMI_FINAL", matchIndex)
              : sfPicks[matchIndex];

            return (
              <div key={`sf-${matchIndex}`} className="flex flex-col gap-2 p-3 bg-brand-surface2 rounded-2xl border border-[#1E4A32]">
                {renderTeamCard(
                  t1 as any,
                  pickedTeam?.teamName === t1?.teamName,
                  () => t1 && handleSfPick(matchIndex, t1 as BracketSlot),
                  !t1,
                  isReadOnly ? pickedTeam?.teamName === t1?.teamName ? (pickedTeam as any)?.isCorrect : null : null
                )}
                <div className="flex justify-center -my-1 relative z-10">
                  <span className="bg-[#1E4A32] text-[#86B59A] text-[10px] font-bold px-2 py-0.5 rounded-full uppercase">
                    VS
                  </span>
                </div>
                {renderTeamCard(
                  t2 as any,
                  pickedTeam?.teamName === t2?.teamName,
                  () => t2 && handleSfPick(matchIndex, t2 as BracketSlot),
                  !t2,
                  isReadOnly ? pickedTeam?.teamName === t2?.teamName ? (pickedTeam as any)?.isCorrect : null : null
                )}
              </div>
            );
          })}
        </div>
      </div>

      <div className="flex flex-col gap-4">
        <h2 className="text-brand-gold text-xs font-bold uppercase tracking-widest text-center">
          Final
        </h2>
        <div className="flex justify-center">
          <div className="flex flex-col gap-2 p-3 bg-brand-surface2 rounded-2xl border border-[#1E4A32] w-full max-w-sm">
            {(() => {
              const t1 = isReadOnly ? getEntryPick("SEMI_FINAL", 0) : sfPicks[0];
              const t2 = isReadOnly ? getEntryPick("SEMI_FINAL", 1) : sfPicks[1];
              const pickedTeam = isReadOnly ? getEntryPick("FINAL", 0) : finalPick;

              return (
                <>
                  {renderTeamCard(
                    t1 as any,
                    pickedTeam?.teamName === t1?.teamName,
                    () => t1 && handleFinalPick(t1 as BracketSlot),
                    !t1,
                    isReadOnly ? pickedTeam?.teamName === t1?.teamName ? (pickedTeam as any)?.isCorrect : null : null
                  )}
                  <div className="flex justify-center -my-1 relative z-10">
                    <span className="bg-[#1E4A32] text-[#86B59A] text-[10px] font-bold px-2 py-0.5 rounded-full uppercase">
                      VS
                    </span>
                  </div>
                  {renderTeamCard(
                    t2 as any,
                    pickedTeam?.teamName === t2?.teamName,
                    () => t2 && handleFinalPick(t2 as BracketSlot),
                    !t2,
                    isReadOnly ? pickedTeam?.teamName === t2?.teamName ? (pickedTeam as any)?.isCorrect : null : null
                  )}
                </>
              );
            })()}
          </div>
        </div>
      </div>

      {!isReadOnly && (
        <div className="flex justify-center pt-4">
          <button
            onClick={handleSubmit}
            disabled={!allPicked || isSubmitting}
            className="w-full max-w-sm h-12 rounded-xl bg-brand-gold text-brand-green font-bold text-sm uppercase tracking-widest hover:bg-[#FFE033] active:scale-95 transition-all duration-150 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isSubmitting ? "Enviando..." : "Confirmar Pick'em"}
          </button>
        </div>
      )}
    </div>
  );
}
