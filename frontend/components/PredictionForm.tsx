"use client";

import { useState } from "react";
import { toast } from "sonner";
import { api } from "@/lib/api";
import { useIdentity } from "@/components/IdentityProvider";

interface Props {
  matchId: number;
  onSuccess: () => void;
}

export function PredictionForm({ matchId, onSuccess }: Props) {
  const { identity } = useIdentity();
  const [home, setHome] = useState<string>("");
  const [away, setAway] = useState<string>("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    const h = parseInt(home, 10);
    const a = parseInt(away, 10);
    if (isNaN(h) || isNaN(a) || h < 0 || a < 0) {
      toast.error("Palpite inválido. Use números ≥ 0.");
      return;
    }
    if (!identity) return;

    setLoading(true);
    try {
      await api.submitPrediction({
        participantId: identity.participantId,
        matchId,
        predictedHomeScore: h,
        predictedAwayScore: a,
      });
      toast.success("Palpite enviado!");
      onSuccess();
    } catch (err) {
      const msg = err instanceof Error ? err.message : "Erro ao enviar palpite.";
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex items-center gap-2 pt-3 border-t border-[#1E4A32]">
      <span className="text-[#86B59A] text-xs font-semibold uppercase tracking-widest flex-none">
        Seu palpite
      </span>
      <div className="flex items-center gap-2 flex-1 justify-center">
        <input
          id={`pred-home-${matchId}`}
          type="number"
          min={0}
          max={99}
          value={home}
          onChange={(e) => setHome(e.target.value)}
          placeholder="0"
          className="w-12 h-10 text-center text-lg font-bold bg-[#1A3D2B] border border-[#1E4A32] rounded-lg text-[#E8F5E9] focus:outline-none focus:border-[#FFD600] transition-colors [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
        />
        <span className="text-[#86B59A] font-bold">×</span>
        <input
          id={`pred-away-${matchId}`}
          type="number"
          min={0}
          max={99}
          value={away}
          onChange={(e) => setAway(e.target.value)}
          placeholder="0"
          className="w-12 h-10 text-center text-lg font-bold bg-[#1A3D2B] border border-[#1E4A32] rounded-lg text-[#E8F5E9] focus:outline-none focus:border-[#FFD600] transition-colors [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
        />
      </div>
      <button
        id={`pred-submit-${matchId}`}
        onClick={handleSubmit}
        disabled={loading}
        className="touch-target flex items-center px-4 rounded-lg bg-[#FFD600] text-[#0A2E1E] font-bold text-sm hover:bg-[#FFE033] active:scale-95 transition-all duration-150 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {loading ? "..." : "Palpitar"}
      </button>
    </div>
  );
}
