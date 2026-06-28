"use client";

import { useState } from "react";
import { toast } from "sonner";
import { api } from "@/lib/api";
import { useIdentity } from "@/components/IdentityProvider";
import type { Match } from "@/types";

interface Props {
  match: Match;
  onSuccess: (home: number, away: number, penaltyWinnerTeam: "HOME" | "AWAY" | null) => void;
}

export function PredictionForm({ match, onSuccess }: Props) {
  const { identity } = useIdentity();
  const [home, setHome] = useState<string>("");
  const [away, setAway] = useState<string>("");
  const [penaltyWinnerTeam, setPenaltyWinnerTeam] = useState<"HOME" | "AWAY" | null>(null);
  const [loading, setLoading] = useState(false);

  const isDraw = home !== "" && away !== "" && home === away;

  const handleSubmit = async () => {
    const h = parseInt(home, 10);
    const a = parseInt(away, 10);
    if (isNaN(h) || isNaN(a) || h < 0 || a < 0) {
      toast.error("Palpite inválido. Use números ≥ 0.");
      return;
    }
    
    let finalPenaltyWinnerTeam = null;
    if (h === a) {
      if (!penaltyWinnerTeam) {
        toast.error("Por favor, selecione o vencedor dos pênaltis.");
        return;
      }
      finalPenaltyWinnerTeam = penaltyWinnerTeam;
    }

    if (!identity) return;

    setLoading(true);
    try {
      await api.submitPrediction({
        participantId: identity.participantId,
        matchId: match.id,
        predictedHomeScore: h,
        predictedAwayScore: a,
        penaltyWinnerTeam: finalPenaltyWinnerTeam,
      });
      toast.success("Palpite enviado!");
      onSuccess(h, a, finalPenaltyWinnerTeam);
    } catch (err) {
      const msg = err instanceof Error ? err.message : "Erro ao enviar palpite.";
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  const isSubmitDisabled = loading || (isDraw && !penaltyWinnerTeam);

  return (
    <div className="flex flex-col gap-3 pt-3 border-t border-[#1E4A32]">
      <div className="flex items-center gap-2">
        <span className="text-[#86B59A] text-xs font-semibold uppercase tracking-widest flex-none">
          Seu palpite
        </span>
        <div className="flex items-center gap-2 flex-1 justify-center">
          <input
            id={`pred-home-${match.id}`}
            type="number"
            min={0}
            max={99}
            value={home}
            onChange={(e) => { setHome(e.target.value); if (e.target.value !== away) setPenaltyWinnerTeam(null); }}
            placeholder="0"
            className="w-12 h-10 text-center text-lg font-bold bg-brand-surface2 border border-[#1E4A32] rounded-lg text-[#E8F5E9] focus:outline-none focus:border-brand-gold transition-colors [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
          />
          <span className="text-[#86B59A] font-bold">×</span>
          <input
            id={`pred-away-${match.id}`}
            type="number"
            min={0}
            max={99}
            value={away}
            onChange={(e) => { setAway(e.target.value); if (e.target.value !== home) setPenaltyWinnerTeam(null); }}
            placeholder="0"
            className="w-12 h-10 text-center text-lg font-bold bg-brand-surface2 border border-[#1E4A32] rounded-lg text-[#E8F5E9] focus:outline-none focus:border-brand-gold transition-colors [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
          />
        </div>
        <button
          id={`pred-submit-${match.id}`}
          onClick={handleSubmit}
          disabled={isSubmitDisabled}
          className="touch-target flex items-center px-4 rounded-lg bg-brand-gold text-brand-green font-bold text-sm hover:bg-[#FFE033] active:scale-95 transition-all duration-150 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? "..." : "Palpitar"}
        </button>
      </div>

      {isDraw && (
        <div className="flex flex-col gap-2 p-3 bg-brand-surface2 rounded-xl border border-brand-gold/30">
          <span className="text-brand-gold text-xs font-semibold uppercase tracking-widest text-center">
            Vencedor nos Pênaltis
          </span>
          <div className="grid grid-cols-2 gap-2">
            <button
              onClick={() => setPenaltyWinnerTeam("HOME")}
              className={`flex items-center justify-center gap-2 py-2 px-2 rounded-lg border transition-all ${
                penaltyWinnerTeam === "HOME"
                  ? "border-brand-gold bg-brand-gold/10"
                  : "border-[#1E4A32] hover:border-brand-gold/50"
              }`}
            >
              {match.homeTeamFlag && (
                <img src={match.homeTeamFlag} alt="" className="w-5 h-5 rounded-full object-cover flex-none" />
              )}
              <span className={`text-xs font-semibold truncate ${penaltyWinnerTeam === "HOME" ? "text-brand-gold" : "text-[#E8F5E9]"}`}>
                {match.homeTeam}
              </span>
            </button>
            <button
              onClick={() => setPenaltyWinnerTeam("AWAY")}
              className={`flex items-center justify-center gap-2 py-2 px-2 rounded-lg border transition-all ${
                penaltyWinnerTeam === "AWAY"
                  ? "border-brand-gold bg-brand-gold/10"
                  : "border-[#1E4A32] hover:border-brand-gold/50"
              }`}
            >
              {match.awayTeamFlag && (
                <img src={match.awayTeamFlag} alt="" className="w-5 h-5 rounded-full object-cover flex-none" />
              )}
              <span className={`text-xs font-semibold truncate ${penaltyWinnerTeam === "AWAY" ? "text-brand-gold" : "text-[#E8F5E9]"}`}>
                {match.awayTeam}
              </span>
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
