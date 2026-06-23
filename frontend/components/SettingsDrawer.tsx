"use client";

import { useState } from "react";
import { LogOut, Eye } from "lucide-react";
import { toast } from "sonner";
import { useIdentity } from "@/components/IdentityProvider";
import { api, todayDateParam } from "@/lib/api";

interface Props {
  open: boolean;
  onClose: () => void;
}

export function SettingsDrawer({ open, onClose }: Props) {
  const { identity, logout } = useIdentity();
  const [revealing, setRevealing] = useState(false);

  const handleLogout = () => {
    logout();
    onClose();
  };

  const handleReveal = async () => {
    setRevealing(true);
    try {
      const date = todayDateParam();
      await api.revealPredictions(date);
      toast.success("Palpites de hoje revelados!");
      onClose();
    } catch {
      toast.error("Não foi possível revelar os palpites.");
    } finally {
      setRevealing(false);
    }
  };

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center"
      onClick={onClose}
    >
      <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" />
      <div
        className="relative w-full max-w-lg bg-brand-surface rounded-t-3xl p-6 pb-10 animate-in slide-in-from-bottom-8 duration-300"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="mx-auto w-10 h-1 rounded-full bg-[#1E4A32] mb-6" />

        <h2 className="font-display text-brand-gold text-2xl tracking-wider mb-1">
          CONFIGURAÇÕES
        </h2>
        <p className="text-[#86B59A] text-sm mb-6">
          Logado como{" "}
          <span className="text-[#E8F5E9] font-semibold">
            {identity?.participantName}
          </span>
        </p>

        <div className="flex flex-col gap-3">
          {identity?.isAdmin && (
            <button
              id="reveal-btn"
              onClick={handleReveal}
              disabled={revealing}
              className="touch-target flex items-center gap-3 w-full px-4 rounded-xl bg-brand-surface2 border border-brand-gold/30 hover:border-brand-gold text-brand-gold font-semibold text-sm transition-all duration-150 disabled:opacity-50"
            >
              <Eye size={18} />
              {revealing ? "Revelando..." : "Revelar palpites de hoje"}
            </button>
          )}

          <button
            id="logout-btn"
            onClick={handleLogout}
            className="touch-target flex items-center gap-3 w-full px-4 rounded-xl bg-brand-surface2 border border-[#1E4A32] hover:border-red-500/50 text-[#86B59A] hover:text-red-400 font-semibold text-sm transition-all duration-150"
          >
            <LogOut size={18} />
            Trocar usuário
          </button>
        </div>
      </div>
    </div>
  );
}
